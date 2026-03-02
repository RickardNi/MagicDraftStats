using System.Globalization;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MagicDraftStats.Models;

namespace MagicDraftStats.Services;

public interface IDeckStatsService
{
    DeckPlayStats CalculateDeckStats(DeckFile deck, IEnumerable<Play> plays);
}

public sealed class DeckStatsService : IDeckStatsService
{
    private readonly ConcurrentDictionary<string, DeckPlayStats> _statsCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, HashSet<string>> RoleColorCache = new(StringComparer.Ordinal);

    public DeckPlayStats CalculateDeckStats(DeckFile deck, IEnumerable<Play> plays)
    {
        if (deck.Date is null || deck.PlayerRefId <= 0)
        {
            return DeckPlayStats.Empty;
        }

        var playList = plays as IList<Play> ?? plays.ToList();
        var deckDates = BuildDeckDates(deck);
        var symbols = FoundationsCardCatalog.GetDeckColorIdentitySymbols(deck.Cards);
        var cacheKey = BuildCacheKey(deck, deckDates, symbols, playList);

        if (_statsCache.TryGetValue(cacheKey, out var cachedStats))
        {
            return cachedStats;
        }

        var baseColors = symbols.Where(symbol => !symbol.IsSplash).Select(symbol => symbol.ColorKey).ToHashSet(StringComparer.Ordinal);
        var splashColors = symbols.Where(symbol => symbol.IsSplash).Select(symbol => symbol.ColorKey).ToHashSet(StringComparer.Ordinal);

        var matchedPlays = new List<Play>();
        var wins = 0;
        var losses = 0;
        var winsAsFirstPlayer = 0;
        var lossesAsFirstPlayer = 0;
        var winsAsNonFirstPlayer = 0;
        var lossesAsNonFirstPlayer = 0;

        foreach (var play in plays)
        {
            if (!TryParsePlayDate(play.Date, out var playDate) || !deckDates.Contains(playDate))
            {
                continue;
            }

            var score = play.PlayerScores.FirstOrDefault(playerScore => playerScore.PlayerRefId == deck.PlayerRefId);
            if (score is null)
            {
                continue;
            }

            if (!RoleMatchesDeck(score.Deck, baseColors, splashColors))
            {
                continue;
            }

            matchedPlays.Add(play);

            if (score.IsWinner)
            {
                wins++;
                if (score.IsFirstPlayer)
                {
                    winsAsFirstPlayer++;
                }
                else
                {
                    winsAsNonFirstPlayer++;
                }
            }
            else
            {
                losses++;
                if (score.IsFirstPlayer)
                {
                    lossesAsFirstPlayer++;
                }
                else
                {
                    lossesAsNonFirstPlayer++;
                }
            }
        }

        var stats = new DeckPlayStats(
            matchedPlays,
            wins,
            losses,
            winsAsFirstPlayer,
            lossesAsFirstPlayer,
            winsAsNonFirstPlayer,
            lossesAsNonFirstPlayer);

        if (_statsCache.Count > 5000)
        {
            _statsCache.Clear();
        }

        _statsCache[cacheKey] = stats;
        return stats;
    }

    private static HashSet<DateOnly> BuildDeckDates(DeckFile deck)
    {
        var dates = new HashSet<DateOnly> { deck.Date!.Value };
        foreach (var additionalDate in deck.AdditionalDates)
        {
            dates.Add(additionalDate);
        }

        return dates;
    }

    private static string BuildCacheKey(DeckFile deck, HashSet<DateOnly> deckDates, IReadOnlyList<DeckColorSymbol> symbols, IList<Play> plays)
    {
        var datesKey = string.Join('|', deckDates.OrderBy(date => date).Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        var colorsKey = string.Join('|', symbols
            .OrderBy(symbol => symbol.ColorKey)
            .ThenBy(symbol => symbol.IsSplash)
            .Select(symbol => $"{symbol.ColorKey}:{(symbol.IsSplash ? 'S' : 'M')}")
        );

        return $"{RuntimeHelpers.GetHashCode(plays)}::{deck.PlayerRefId}::{datesKey}::{colorsKey}";
    }

    private static bool TryParsePlayDate(string playDateRaw, out DateOnly playDate)
    {
        if (DateTime.TryParse(playDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDateTime))
        {
            playDate = DateOnly.FromDateTime(parsedDateTime);
            return true;
        }

        if (DateOnly.TryParse(playDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
        {
            playDate = parsedDate;
            return true;
        }

        playDate = default;
        return false;
    }

    private static bool RoleMatchesDeck(string role, HashSet<string> baseColors, HashSet<string> splashColors)
    {
        var roleColors = ParseRoleColors(role);
        if (roleColors.Count == 0)
        {
            return baseColors.Count == 0 && splashColors.Count == 0;
        }

        if (!baseColors.IsSubsetOf(roleColors))
        {
            return false;
        }

        var allowedColors = new HashSet<string>(baseColors, StringComparer.Ordinal);
        allowedColors.UnionWith(splashColors);

        if (!roleColors.IsSubsetOf(allowedColors))
        {
            return false;
        }

        return splashColors.Count > 0 || roleColors.SetEquals(baseColors);
    }

    private static HashSet<string> ParseRoleColors(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (RoleColorCache.TryGetValue(role, out var cachedRoleColors))
        {
            return cachedRoleColors;
        }

        var normalized = role
            .Replace('／', '/')
            .Replace('|', '/')
            .Replace(',', '/');

        var colors = new HashSet<string>(StringComparer.Ordinal);

        foreach (var token in normalized.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var color in ExpandRoleTokenToColors(token))
            {
                colors.Add(color);
            }
        }

        RoleColorCache[role] = colors;
        return colors;
    }

    private static IEnumerable<string> ExpandRoleTokenToColors(string token)
    {
        return token.Trim().ToLowerInvariant() switch
        {
            "white" or "w" => ["W"],
            "blue" or "u" => ["U"],
            "black" or "b" => ["B"],
            "red" or "r" => ["R"],
            "green" or "g" => ["G"],
            "selesnya" => ["W", "G"],
            "boros" => ["W", "R"],
            "azorius" => ["W", "U"],
            "orzhov" => ["W", "B"],
            "simic" => ["U", "G"],
            "dimir" => ["U", "B"],
            "izzet" => ["U", "R"],
            "golgari" => ["B", "G"],
            "rakdos" => ["B", "R"],
            "gruul" => ["R", "G"],
            _ => []
        };
    }
}

public sealed record DeckPlayStats(
    IReadOnlyList<Play> MatchedPlays,
    int Wins,
    int Losses,
    int WinsAsFirstPlayer,
    int LossesAsFirstPlayer,
    int WinsAsNonFirstPlayer,
    int LossesAsNonFirstPlayer)
{
    public static DeckPlayStats Empty { get; } = new([], 0, 0, 0, 0, 0, 0);

    public int Plays => Wins + Losses;

    public double? FirstPlayerWinRate =>
        (WinsAsFirstPlayer + LossesAsFirstPlayer) == 0
            ? null
            : WinsAsFirstPlayer / (double)(WinsAsFirstPlayer + LossesAsFirstPlayer);

    public double? GoingSecondWinRate =>
        (WinsAsNonFirstPlayer + LossesAsNonFirstPlayer) == 0
            ? null
            : WinsAsNonFirstPlayer / (double)(WinsAsNonFirstPlayer + LossesAsNonFirstPlayer);
}