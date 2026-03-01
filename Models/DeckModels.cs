using System.Text.Json.Serialization;

namespace MagicDraftStats.Models;

public class DeckFile
{
    [JsonPropertyName("playerRefId")]
    public int PlayerRefId { get; set; }

    [JsonPropertyName("date")]
    public DateOnly? Date { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("deckSize")]
    public int DeckSize { get; set; }

    [JsonPropertyName("cards")]
    public List<CardEntry> Cards { get; set; } = [];

    [JsonPropertyName("sideboard")]
    public List<CardEntry> Sideboard { get; set; } = [];
}

public class CardEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class DeckIndex
{
    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = [];
}

public enum DeckValidationSeverity
{
    Error,
    Warning
}

public record DeckValidationMessage(DeckValidationSeverity Severity, string Code, string Message);

public class DeckImportItemResult
{
    public string FilePath { get; set; } = string.Empty;
    public DeckFile? Deck { get; set; }
    public List<DeckValidationMessage> ValidationMessages { get; set; } = [];
    public bool IsValid => ValidationMessages.All(m => m.Severity != DeckValidationSeverity.Error);
}

public class DeckBatchImportResult
{
    public List<DeckImportItemResult> Items { get; set; } = [];

    public int TotalFiles => Items.Count;
    public int ValidFiles => Items.Count(i => i.IsValid);
    public int InvalidFiles => Items.Count(i => !i.IsValid);
    public int TotalWarnings => Items.Sum(i => i.ValidationMessages.Count(m => m.Severity == DeckValidationSeverity.Warning));
}
