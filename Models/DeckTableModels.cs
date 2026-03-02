namespace MagicDraftStats.Models;

public class DeckTableRow
{
    public string DeckId { get; set; } = string.Empty;
    public string DeckName { get; set; } = string.Empty;
    public List<DeckColorSymbol> ColorSymbols { get; set; } = new();
    public DateOnly? Date { get; set; }
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int? Rank { get; set; }
    public int Plays { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}

public class DeckValidationIssue
{
    public string FilePath { get; set; } = string.Empty;
    public DeckValidationSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed record DeckTableDataResult(IReadOnlyList<DeckTableRow> DeckRows, IReadOnlyList<DeckValidationIssue> ValidationIssues);