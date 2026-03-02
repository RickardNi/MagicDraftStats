namespace MagicDraftStats.Services;

public interface IGlobalFilterService
{
    event Action? OnFilterChanged;
    HashSet<string> SelectedVariants { get; }
    HashSet<string> SelectedSets { get; }
    bool IncludeUndefinedSets { get; }
    bool IncludeRegularDraft { get; }
    void SetVariantFilter(HashSet<string> variants);
    void SetSetFilter(HashSet<string> sets);
    void SetIncludeUndefinedSets(bool includeUndefinedSets);
    void SetIncludeRegularDraft(bool includeRegularDraft);
    void SetAllAvailableVariants(IEnumerable<string> variants);
    void SetAllAvailableSets(IEnumerable<string> sets);
    void ClearVariantFilter();
    void ClearSetFilter();
    bool IsVariantSelected(string variant);
    bool IsSetSelected(string setVariant);
}

public class GlobalFilterService : IGlobalFilterService
{
    private HashSet<string> _selectedVariants = [];
    private HashSet<string> _selectedSets = [];
    private bool _includeUndefinedSets;
    private bool _includeRegularDraft;

    public event Action? OnFilterChanged;

    public HashSet<string> SelectedVariants => [.. _selectedVariants];
    public HashSet<string> SelectedSets => [.. _selectedSets];
    public bool IncludeUndefinedSets => _includeUndefinedSets;
    public bool IncludeRegularDraft => _includeRegularDraft;

    public void SetVariantFilter(HashSet<string> variants)
    {
        _selectedVariants = new HashSet<string>(variants, StringComparer.OrdinalIgnoreCase);
        OnFilterChanged?.Invoke();
    }

    public void SetSetFilter(HashSet<string> sets)
    {
        _selectedSets = new HashSet<string>(sets, StringComparer.OrdinalIgnoreCase);
        OnFilterChanged?.Invoke();
    }

    public void SetIncludeUndefinedSets(bool includeUndefinedSets)
    {
        _includeUndefinedSets = includeUndefinedSets;
        OnFilterChanged?.Invoke();
    }

    public void SetIncludeRegularDraft(bool includeRegularDraft)
    {
        _includeRegularDraft = includeRegularDraft;
        OnFilterChanged?.Invoke();
    }

    public void SetAllAvailableVariants(IEnumerable<string> variants)
    {
        _selectedVariants = new HashSet<string>(variants, StringComparer.OrdinalIgnoreCase);
        OnFilterChanged?.Invoke();
    }

    public void SetAllAvailableSets(IEnumerable<string> sets)
    {
        _selectedSets = new HashSet<string>(sets, StringComparer.OrdinalIgnoreCase);
        OnFilterChanged?.Invoke();
    }

    public void ClearVariantFilter()
    {
        _selectedVariants.Clear();
        OnFilterChanged?.Invoke();
    }

    public void ClearSetFilter()
    {
        _selectedSets.Clear();
        OnFilterChanged?.Invoke();
    }

    public bool IsVariantSelected(string variant)
    {
        return _selectedVariants.Contains(variant);
    }

    public bool IsSetSelected(string setVariant)
    {
        return _selectedSets.Contains(setVariant);
    }
}

public static class VariantDefinitions
{
    public static readonly HashSet<string> SetVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        "Magic Foundations Cube"
    };

    public const string RequiredVariant = "Draft";

    public static IEnumerable<string> SplitVariantTokens(string? rawVariant)
    {
        if (string.IsNullOrWhiteSpace(rawVariant))
            return [];

        return rawVariant
            .Split(['／'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token));
    }
}