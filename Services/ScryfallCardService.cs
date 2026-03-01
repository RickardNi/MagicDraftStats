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
    public decimal? ManaValue { get; set; }
    public bool IsLand { get; set; }
    public bool IsCreature { get; set; }
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
            var card = await GetCardByNameAsync(entry.Name, setCode);
            return MapToDeckCardVisual(entry.Name, entry.Count, card, setCode);
        });

        var results = await Task.WhenAll(fetchTasks);

        return results
            .OrderBy(card => card.IsLand)
            .ThenBy(card => card.ManaValue ?? decimal.MaxValue)
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
        var imageUrl = card?.ImageUris?.Normal
            ?? card?.CardFaces?.FirstOrDefault(face => !string.IsNullOrWhiteSpace(face.ImageUris?.Normal))?.ImageUris?.Normal
            ?? $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}&set={setCode}&format=image";

        var typeLine = card?.TypeLine ?? string.Empty;
        var isLand = typeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);
        var isCreature = typeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase);

        return new DeckCardVisual
        {
            Name = name,
            Count = count,
            ManaValue = card?.Cmc,
            IsLand = isLand,
            IsCreature = isCreature,
            ImageUrl = imageUrl,
            ScryfallUrl = card?.ScryfallUri
        };
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
