using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagicDraftStats.Models;

namespace MagicDraftStats.Services;

public interface IScryfallCardService
{
    Task<List<DeckCardVisual>> GetDeckCardVisualsAsync(IEnumerable<CardEntry> cards, string setCode = "fdn");
}

public class DeckCardVisual
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int? ManaValue { get; set; }
    public string[] ColorIdentity { get; set; } = [];
    public bool IsLand { get; set; }
    public bool IsBasicLand { get; set; }
    public bool IsCreature { get; set; }
    public bool IsPlaneswalker { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ScryfallUrl { get; set; }
}

public class ScryfallCardService(HttpClient httpClient, ILogger<ScryfallCardService> logger) : IScryfallCardService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ScryfallCardService> _logger = logger;
    private readonly ConcurrentDictionary<string, ScryfallCard?> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, string> FullArtPlaneswalkerCollectorNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ajani, Caller of the Pride"] = "357",
        ["Chandra, Flameshaper"] = "360",
        ["Kaito, Cunning Infiltrator"] = "358",
        ["Liliana, Dreadhorde General"] = "359",
        ["Vivien Reid"] = "361"
    };

    public async Task<List<DeckCardVisual>> GetDeckCardVisualsAsync(IEnumerable<CardEntry> cards, string setCode = "fdn")
    {
        var entries = cards
            .Where(card => !string.IsNullOrWhiteSpace(card.Name) && card.Count > 0)
            .GroupBy(card => card.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new { Name = group.Key, Count = group.Sum(item => item.Count) })
            .ToList();

        var fetchTasks = entries.Select(async entry =>
        {
            if (string.Equals(setCode, "fdn", StringComparison.OrdinalIgnoreCase)
                && FoundationsCardCatalog.TryGetMetadata(entry.Name, out var metadata))
            {
                return MapToDeckCardVisualFromCatalog(entry.Name, entry.Count, metadata, setCode);
            }

            var card = await GetCardByNameAsync(entry.Name, setCode);
            return MapToDeckCardVisual(entry.Name, entry.Count, card, setCode);
        });

        var results = await Task.WhenAll(fetchTasks);

        return results
            .OrderBy(card => card.IsLand)
            .ThenBy(card => card.ManaValue ?? int.MaxValue)
            .ThenBy(card => card.Name)
            .ToList();
    }

    private async Task<ScryfallCard?> GetCardByNameAsync(string cardName, string setCode)
    {
        if (FullArtPlaneswalkerCollectorNumbers.TryGetValue(cardName, out var collectorNumber))
        {
            return await GetCardByCollectorNumberAsync(setCode, collectorNumber, cardName);
        }

        var cacheKey = $"{setCode}:{cardName}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var encodedName = Uri.EscapeDataString(cardName);
            var url = $"https://api.scryfall.com/cards/named?exact={encodedName}&set={setCode}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Scryfall lookup failed for card '{CardName}' in set '{SetCode}' with status {StatusCode}", cardName, setCode, (int)response.StatusCode);
                _cache.TryAdd(cacheKey, null);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            var card = await JsonSerializer.DeserializeAsync<ScryfallCard>(stream, JsonOptions);
            _cache.TryAdd(cacheKey, card);
            return card;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scryfall lookup exception for card '{CardName}' in set '{SetCode}'", cardName, setCode);
            _cache.TryAdd(cacheKey, null);
            return null;
        }
    }

    private async Task<ScryfallCard?> GetCardByCollectorNumberAsync(string setCode, string collectorNumber, string cardNameForFallback)
    {
        var cacheKey = $"{setCode}:collector:{collectorNumber}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var url = $"https://api.scryfall.com/cards/{setCode}/{collectorNumber}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Scryfall collector lookup failed for card '{CardName}' in set '{SetCode}' with collector number {CollectorNumber} and status {StatusCode}", cardNameForFallback, setCode, collectorNumber, (int)response.StatusCode);
                _cache.TryAdd(cacheKey, null);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            var card = await JsonSerializer.DeserializeAsync<ScryfallCard>(stream, JsonOptions);
            _cache.TryAdd(cacheKey, card);
            return card;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scryfall collector lookup exception for card '{CardName}' in set '{SetCode}' with collector number {CollectorNumber}", cardNameForFallback, setCode, collectorNumber);
            _cache.TryAdd(cacheKey, null);
            return null;
        }
    }

    private static DeckCardVisual MapToDeckCardVisual(string name, int count, ScryfallCard? card, string setCode)
    {
        var imageUrl = GetImageUrl(name, setCode, card);
        var scryfallUrl = GetScryfallUrl(name, setCode, card?.ScryfallUri);

        var typeLine = card?.TypeLine ?? string.Empty;
        var isLand = typeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);
        var isBasicLand = typeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase)
            && typeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);
        var isCreature = typeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase);
        var isPlaneswalker = typeLine.Contains("Planeswalker", StringComparison.OrdinalIgnoreCase);
        var colorIdentity = card?.ColorIdentity?.ToArray() ?? [];

        return new DeckCardVisual
        {
            Name = name,
            Count = count,
            ManaValue = card != null ? (int)Math.Round(card.Cmc, MidpointRounding.AwayFromZero) : null,
            ColorIdentity = colorIdentity,
            IsLand = isLand,
            IsBasicLand = isBasicLand,
            IsCreature = isCreature,
            IsPlaneswalker = isPlaneswalker,
            ImageUrl = imageUrl,
            ScryfallUrl = scryfallUrl
        };
    }

    private static DeckCardVisual MapToDeckCardVisualFromCatalog(string name, int count, FoundationsCardMetadata metadata, string setCode)
    {
        var isLand = HasType(metadata.Types, "Land");
        var isBasicLand = HasType(metadata.Types, "Basic") && isLand;
        var isCreature = HasType(metadata.Types, "Creature");
        var isPlaneswalker = HasType(metadata.Types, "Planeswalker");

        return new DeckCardVisual
        {
            Name = name,
            Count = count,
            ManaValue = FoundationsCardCatalog.GetManaValueFromManaCost(metadata.ManaCost),
            ColorIdentity = FoundationsCardCatalog.GetColorIdentityFromManaCost(metadata.ManaCost).ToArray(),
            IsLand = isLand,
            IsBasicLand = isBasicLand,
            IsCreature = isCreature,
            IsPlaneswalker = isPlaneswalker,
            ImageUrl = GetImageUrl(name, setCode, null),
            ScryfallUrl = GetScryfallUrl(name, setCode, null)
        };
    }

    private static string GetImageUrl(string name, string setCode, ScryfallCard? card)
    {
        if (FullArtPlaneswalkerCollectorNumbers.TryGetValue(name, out var collectorNumber))
        {
            return $"https://api.scryfall.com/cards/{setCode}/{collectorNumber}?format=image&version=normal";
        }

        return card?.ImageUris?.Normal
               ?? card?.CardFaces?.FirstOrDefault(face => !string.IsNullOrWhiteSpace(face.ImageUris?.Normal))?.ImageUris?.Normal
               ?? $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}&set={setCode}&format=image";
    }

    private static string GetScryfallUrl(string name, string setCode, string? fromApi)
    {
        if (FullArtPlaneswalkerCollectorNumbers.TryGetValue(name, out var collectorNumber))
        {
            return $"https://scryfall.com/card/{setCode}/{collectorNumber}";
        }

        if (!string.IsNullOrWhiteSpace(fromApi))
        {
            return fromApi;
        }

        var query = Uri.EscapeDataString($"!\"{name}\" set:{setCode}");
        return $"https://scryfall.com/search?q={query}";
    }

    private static bool HasType(IEnumerable<string> types, string typeName)
    {
        return types.Any(type =>
            type.Equals(typeName, StringComparison.OrdinalIgnoreCase)
            || type.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(token => token.Equals(typeName, StringComparison.OrdinalIgnoreCase)));
    }

    private class ScryfallCard
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("cmc")]
        public decimal Cmc { get; set; }

        [JsonPropertyName("type_line")]
        public string TypeLine { get; set; } = string.Empty;

        [JsonPropertyName("scryfall_uri")]
        public string? ScryfallUri { get; set; }

        [JsonPropertyName("color_identity")]
        public List<string>? ColorIdentity { get; set; }

        [JsonPropertyName("image_uris")]
        public ScryfallImageUris? ImageUris { get; set; }

        [JsonPropertyName("card_faces")]
        public List<ScryfallCardFace>? CardFaces { get; set; }
    }

    private class ScryfallCardFace
    {
        [JsonPropertyName("image_uris")]
        public ScryfallImageUris? ImageUris { get; set; }
    }

    private class ScryfallImageUris
    {
        [JsonPropertyName("normal")]
        public string? Normal { get; set; }
    }
}
