using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using MagicDraftStats.Models;

namespace MagicDraftStats.Services;

public interface IDeckImportService
{
    Task<DeckBatchImportResult> LoadAndValidateDecksAsync(string manifestPath = "data/decks.json");
    void ResetCache();
}

public class DeckImportService : IDeckImportService
{
    private const string DeckFilesFolder = "data/decks";
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeckImportService> _logger;
    private readonly IBGStatsImportService _bgStatsImportService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // Cache fields
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DeckIndex? _cachedIndex;
    private readonly ConcurrentDictionary<string, CachedDeckFile> _cachedDecks = new(StringComparer.OrdinalIgnoreCase);

    private class CachedDeckFile
    {
        public DeckFile? Deck { get; set; }
        public List<DeckValidationMessage> LoadErrors { get; set; } = [];
    }

    public DeckImportService(HttpClient httpClient, ILogger<DeckImportService> logger, IBGStatsImportService bgStatsImportService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _bgStatsImportService = bgStatsImportService;
        
        // Listen to BGStats data import event to clear the validation cache/reset if players database changes
        _bgStatsImportService.OnDataImported += ResetCache;
    }

    public void ResetCache()
    {
        _cachedIndex = null;
        _cachedDecks.Clear();
        _logger.LogInformation("DeckImportService cache has been reset.");
    }

    public async Task<DeckBatchImportResult> LoadAndValidateDecksAsync(string manifestPath = "data/decks.json")
    {
        var result = new DeckBatchImportResult();
        var bgStatsData = await _bgStatsImportService.GetCurrentDataAsync();
        var validPlayerIds = bgStatsData?.Players?
            .Select(player => player.Id)
            .Where(playerId => playerId > 0)
            .ToHashSet() ?? [];

        DeckIndex? index;
        await _lock.WaitAsync();
        try
        {
            if (_cachedIndex != null)
            {
                index = _cachedIndex;
            }
            else
            {
                try
                {
                    var indexJson = await _httpClient.GetStringAsync(manifestPath);
                    index = JsonSerializer.Deserialize<DeckIndex>(indexJson, JsonOptions);
                    _cachedIndex = index;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not read deck manifest file at {ManifestPath}", manifestPath);
                    result.Items.Add(new DeckImportItemResult
                    {
                        FilePath = manifestPath,
                        ValidationMessages =
                        [
                            new DeckValidationMessage(
                                DeckValidationSeverity.Error,
                                "MANIFEST_READ_FAILED",
                                $"Could not read deck manifest file: {manifestPath}")
                        ]
                    });

                    return result;
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        if (index?.Files == null || index.Files.Count == 0)
        {
            _logger.LogWarning("Deck manifest file {ManifestPath} has no files", manifestPath);
            return result;
        }

        var manifestEntries = index.Files
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Process file fetching and deserialization in parallel
        var tasks = manifestEntries.Select(async manifestEntry =>
        {
            var resolvedPath = ResolveDeckPath(manifestEntry);
            
            if (_cachedDecks.TryGetValue(resolvedPath, out var cached))
            {
                return (resolvedPath, cached);
            }

            var cachedEntry = await LoadSingleDeckRawAsync(resolvedPath);
            _cachedDecks.TryAdd(resolvedPath, cachedEntry);
            return (resolvedPath, cachedEntry);
        });

        var loadedEntries = await Task.WhenAll(tasks);

        // Run validations sequentially in memory (which is extremely fast and avoids concurrency issues on shared objects)
        foreach (var (resolvedPath, cachedEntry) in loadedEntries)
        {
            var item = new DeckImportItemResult
            {
                FilePath = resolvedPath,
                Deck = cachedEntry.Deck
            };
            item.ValidationMessages.AddRange(cachedEntry.LoadErrors);
            if (cachedEntry.Deck != null)
            {
                item.ValidationMessages.AddRange(ValidateDeck(resolvedPath, cachedEntry.Deck, validPlayerIds));
            }
            result.Items.Add(item);
        }

        return result;
    }

    private static string ResolveDeckPath(string manifestEntry)
    {
        var trimmed = manifestEntry.Trim();
        if (trimmed.Contains('/') || trimmed.Contains('\\'))
            return trimmed;

        return $"{DeckFilesFolder}/{trimmed}";
    }

    private async Task<CachedDeckFile> LoadSingleDeckRawAsync(string filePath)
    {
        var entry = new CachedDeckFile();

        string jsonContent;
        try
        {
            jsonContent = await _httpClient.GetStringAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read deck file {FilePath}", filePath);
            entry.LoadErrors.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "FILE_READ_FAILED",
                $"Could not read file: {filePath}"));
            return entry;
        }

        DeckFile? deck;
        try
        {
            deck = JsonSerializer.Deserialize<DeckFile>(jsonContent, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize deck file {FilePath}", filePath);
            entry.LoadErrors.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "DESERIALIZE_FAILED",
                $"JSON deserialization failed for file: {filePath}"));
            return entry;
        }

        if (deck == null)
        {
            entry.LoadErrors.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "EMPTY_DECK",
                "Deserialization produced an empty deck object."));
            return entry;
        }

        entry.Deck = deck;
        return entry;
    }

    private static IEnumerable<DeckValidationMessage> ValidateDeck(string filePath, DeckFile deck, HashSet<int> validPlayerIds)
    {
        var messages = new List<DeckValidationMessage>();

        if (deck.PlayerRefId <= 0)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "PLAYER_REF_ID_INVALID",
                "The 'playerRefId' field is required and must be greater than 0."));
        }
        else if (validPlayerIds.Count == 0)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "BGSTATS_PLAYERS_UNAVAILABLE",
                "Could not validate 'playerRefId' because no players were available from BGStats data."));
        }
        else if (!validPlayerIds.Contains(deck.PlayerRefId))
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "PLAYER_REF_ID_NOT_FOUND",
                $"playerRefId {deck.PlayerRefId} does not exist in BGStats players."));
        }

        if (deck.Rank <= 0)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "RANK_REQUIRED",
                "The 'rank' field is required and must be greater than 0."));
        }

        if (deck.PlayerCount <= 0)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "PLAYER_COUNT_REQUIRED",
                "The 'playerCount' field is required and must be greater than 0."));
        }

        if (!deck.Date.HasValue)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "DATE_REQUIRED",
                "The 'date' field is required."));
        }
        else
        {
            ValidateDateAgainstFilename(filePath, deck.Date.Value, messages);
        }

        var mainDeckCardCount = deck.Cards.Sum(card => card.Count);
        if (deck.DeckSize != mainDeckCardCount)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "DECKSIZE_MISMATCH",
                $"deckSize is {deck.DeckSize}, but sum(cards.count) is {mainDeckCardCount}."));
        }

        if (deck.DeckSize != 40)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Warning,
                "DECKSIZE_NOT_40",
                $"deckSize is {deck.DeckSize}. Draft decks are usually expected to be 40."));
        }

        ValidateCardNames("cards", deck.Cards, messages);
        ValidateCardNames("sideboard", deck.Sideboard, messages);

        return messages;
    }

    private static void ValidateDateAgainstFilename(string filePath, DateOnly date, List<DeckValidationMessage> messages)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension) || fileNameWithoutExtension.Length < 10)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "FILENAME_DATE_MISSING",
                "Filename must start with a date in yyyy-MM-dd format."));
            return;
        }

        var prefix = fileNameWithoutExtension[..10];
        if (!DateOnly.TryParseExact(prefix, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "FILENAME_DATE_INVALID",
                "Filename must start with a valid date in yyyy-MM-dd format."));
            return;
        }

        if (fileDate != date)
        {
            messages.Add(new DeckValidationMessage(
                DeckValidationSeverity.Error,
                "DATE_FILENAME_MISMATCH",
                $"Filename date ({fileDate:yyyy-MM-dd}) does not match JSON 'date' ({date:yyyy-MM-dd})."));
        }
    }

    private static void ValidateCardNames(string areaName, IEnumerable<CardEntry> cards, List<DeckValidationMessage> messages)
    {
        foreach (var card in cards)
        {
            if (string.IsNullOrWhiteSpace(card.Name))
            {
                messages.Add(new DeckValidationMessage(
                    DeckValidationSeverity.Error,
                    "CARD_NAME_REQUIRED",
                    $"Card name is missing in {areaName}."));
                continue;
            }

            if (card.Count <= 0)
            {
                messages.Add(new DeckValidationMessage(
                    DeckValidationSeverity.Error,
                    "CARD_COUNT_INVALID",
                    $"Invalid count ({card.Count}) for '{card.Name}' in {areaName}."));
            }

            if (!FoundationsCardCatalog.Contains(card.Name))
            {
                messages.Add(new DeckValidationMessage(
                    DeckValidationSeverity.Error,
                    "INVALID_FOUNDATIONS_CARD",
                    $"'{card.Name}' is not found in the Foundations card catalog ({areaName})."));
            }
        }
    }
}
