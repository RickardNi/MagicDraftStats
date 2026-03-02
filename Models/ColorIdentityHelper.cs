using System.Text.RegularExpressions;

namespace MagicDraftStats.Models;

public record DeckColorSymbol(string ColorKey, bool IsSplash);

public static class ColorIdentityHelper
{
    private static readonly Dictionary<string, string> ColorNameToKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["White"] = "W",
        ["Blue"] = "U",
        ["Black"] = "B",
        ["Red"] = "R",
        ["Green"] = "G"
    };

    public static IReadOnlyList<string> GetRoleColorIdentityKeys(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return [];
        }

        var colorKeys = role
            .Split(['／', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(MapRoleTokenToColorKeys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(GetColorOrder)
            .ToList();

        if (colorKeys.Count > 0)
        {
            return colorKeys;
        }

        return ColorNameToKey
            .Where(entry => Regex.IsMatch(role, $@"\b{Regex.Escape(entry.Key)}\b", RegexOptions.IgnoreCase))
            .Select(entry => entry.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(GetColorOrder)
            .ToList();
    }

    public static IReadOnlyList<DeckColorSymbol> GetRoleColorSymbols(string? role)
    {
        return GetRoleColorIdentityKeys(role)
            .Select(colorKey => new DeckColorSymbol(colorKey, false))
            .ToList();
    }

    public static string GetRoleColorIdentityName(string? role)
    {
        return GetColorIdentityName(GetRoleColorIdentityKeys(role));
    }

    public static string GetColorIdentityName(IReadOnlyList<string> orderedColors, IReadOnlyDictionary<string, int>? colorCardCounts = null)
    {
        if (orderedColors.Count == 0)
        {
            return "Colorless";
        }

        if (orderedColors.Count == 1)
        {
            return $"Mono {GetColorName(orderedColors[0])}";
        }

        if (orderedColors.Count == 2)
        {
            return GetTwoColorName(orderedColors[0], orderedColors[1]);
        }

        if (orderedColors.Count == 3 && colorCardCounts is not null)
        {
            var splashColor = orderedColors[2];
            if (colorCardCounts.TryGetValue(splashColor, out var splashCards) && splashCards == 1)
            {
                return $"{GetTwoColorName(orderedColors[0], orderedColors[1])}+";
            }
        }

        return string.Concat(orderedColors);
    }

    public static int GetColorOrder(string color) => color switch
    {
        "W" => 0,
        "U" => 1,
        "B" => 2,
        "R" => 3,
        "G" => 4,
        _ => 99
    };

    public static string GetColorName(string color) => color switch
    {
        "W" => "White",
        "U" => "Blue",
        "B" => "Black",
        "R" => "Red",
        "G" => "Green",
        _ => "Colorless"
    };

    public static string GetTwoColorName(string colorA, string colorB)
    {
        var pair = string.Concat(new string[] { colorA, colorB }.OrderBy(GetColorOrder));
        return pair switch
        {
            "WG" => "Selesnya",
            "WR" => "Boros",
            "WU" => "Azorius",
            "WB" => "Orzhov",
            "UG" => "Simic",
            "UB" => "Dimir",
            "UR" => "Izzet",
            "BG" => "Golgari",
            "BR" => "Rakdos",
            "RG" => "Gruul",
            _ => pair
        };
    }

    private static IEnumerable<string> MapRoleTokenToColorKeys(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            yield break;
        }

        token = token.Trim();

        if (ColorNameToKey.TryGetValue(token, out var namedColorKey))
        {
            yield return namedColorKey;
            yield break;
        }

        var compactToken = token.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (!compactToken.All(character => character is 'W' or 'U' or 'B' or 'R' or 'G'))
        {
            yield break;
        }

        foreach (var character in compactToken)
        {
            yield return character.ToString();
        }
    }
}