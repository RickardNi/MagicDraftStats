using MagicDraftStats.Models;

namespace MagicDraftStats.Services;

public interface IDeckTableDataService
{
    Task<DeckTableDataResult> LoadAsync(IEnumerable<Play>? plays = null);
}

public sealed class DeckTableDataService : IDeckTableDataService
{
    private readonly IDeckImportService _deckImportService;
    private readonly IBGStatsImportService _bgStatsService;
    private readonly IDeckStatsService _deckStatsService;

    public DeckTableDataService(
        IDeckImportService deckImportService,
        IBGStatsImportService bgStatsService,
        IDeckStatsService deckStatsService)
    {
        _deckImportService = deckImportService;
        _bgStatsService = bgStatsService;
        _deckStatsService = deckStatsService;
    }

    public async Task<DeckTableDataResult> LoadAsync(IEnumerable<Play>? plays = null)
    {
        var importResult = await _deckImportService.LoadAndValidateDecksAsync();
        var bgStatsData = await _bgStatsService.GetCurrentDataAsync();
        var playerLookup = bgStatsData?.Players?
            .ToDictionary(player => player.Id, player => player.Name)
            ?? new Dictionary<int, string>();

        var allPlays = plays?.ToList() ?? bgStatsData?.Plays ?? [];

        var deckRows = importResult.Items
            .Where(item => item.Deck != null)
            .Select(item =>
            {
                var deck = item.Deck!;
                var stats = _deckStatsService.CalculateDeckStats(deck, allPlays);

                return new DeckTableRow
                {
                    DeckId = Path.GetFileNameWithoutExtension(item.FilePath),
                    Date = deck.Date,
                    DeckName = FoundationsCardCatalog.GetDeckColorIdentityName(deck.Cards),
                    ColorSymbols = FoundationsCardCatalog.GetDeckColorIdentitySymbols(deck.Cards).ToList(),
                    PlayerId = deck.PlayerRefId,
                    PlayerName = playerLookup.TryGetValue(deck.PlayerRefId, out var playerName) ? playerName : $"Player #{deck.PlayerRefId}",
                    Rank = deck.Rank,
                    Plays = stats.Plays,
                    Wins = stats.Wins,
                    Losses = stats.Losses
                };
            })
            .OrderByDescending(deck => deck.Date)
            .ThenByDescending(deck => deck.Wins)
            .ThenBy(deck => deck.PlayerName)
            .ToList();

        var validationIssues = importResult.Items
            .SelectMany(item => item.ValidationMessages.Select(message => new DeckValidationIssue
            {
                FilePath = item.FilePath,
                Severity = message.Severity,
                Code = message.Code,
                Message = message.Message
            }))
            .OrderByDescending(issue => issue.Severity == DeckValidationSeverity.Error)
            .ThenBy(issue => issue.FilePath)
            .ThenBy(issue => issue.Code)
            .ToList();

        return new DeckTableDataResult(deckRows, validationIssues);
    }
}