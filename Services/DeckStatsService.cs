using System.Globalization;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MagicDraftStats.Models;

namespace MagicDraftStats.Services;

public interface IDeckStatsService
{
    DeckPlayStats CalculateDeckStats(DeckFile deck, IEnumerable<Play> plays);
    bool IsRoleMatchForDeck(DeckFile deck, string role);
    IReadOnlyList<PlayerScoreColorStatEntry> BuildPlayerScoreColorEntries(IEnumerable<Play> plays, Func<PlayerScore, bool> scorePredicate);
    IReadOnlyList<ColorIdentityStatRow> AggregateByColorIdentity(IEnumerable<PlayerScoreColorStatEntry> entries, Func<PlayerScoreColorStatEntry, bool> isWinPredicate);
    ColorInclusionStat? GetMostIncludedColor(IEnumerable<PlayerScoreColorStatEntry> entries);
    IReadOnlyList<ColorIdentityStatRow> CalculateOpponentColorIdentityStats(IEnumerable<Play> plays, int playerId);
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

    public bool IsRoleMatchForDeck(DeckFile deck, string role)
    {
        var symbols = FoundationsCardCatalog.GetDeckColorIdentitySymbols(deck.Cards);
        var baseColors = symbols.Where(symbol => !symbol.IsSplash).Select(symbol => symbol.ColorKey).ToHashSet(StringComparer.Ordinal);
        var splashColors = symbols.Where(symbol => symbol.IsSplash).Select(symbol => symbol.ColorKey).ToHashSet(StringComparer.Ordinal);

        return RoleMatchesDeck(role, baseColors, splashColors);
    }

    public IReadOnlyList<PlayerScoreColorStatEntry> BuildPlayerScoreColorEntries(IEnumerable<Play> plays, Func<PlayerScore, bool> scorePredicate)
    {
        var entries = new List<PlayerScoreColorStatEntry>();

        foreach (var play in plays)
        {
            var playedAt = TryParsePlayDateTime(play.Date, out var parsedPlayedAt) ? parsedPlayedAt : (DateTime?)null;

            foreach (var score in play.PlayerScores.Where(scorePredicate))
            {
                entries.Add(new PlayerScoreColorStatEntry(
                    score,
                    ColorIdentityHelper.GetRoleColorIdentityKeys(score.Deck),
                    playedAt));
            }
        }

        return entries;
    }

    public IReadOnlyList<ColorIdentityStatRow> AggregateByColorIdentity(IEnumerable<PlayerScoreColorStatEntry> entries, Func<PlayerScoreColorStatEntry, bool> isWinPredicate)
    {
        return entries
            .GroupBy(entry => BuildColorIdentityKey(entry.ColorIdentityKeys))
            .Select(group =>
            {
                var keys = group.First().ColorIdentityKeys;
                var plays = group.Count();
                var wins = group.Count(isWinPredicate);
                var losses = plays - wins;

                return new ColorIdentityStatRow(
                    ColorIdentityName: ColorIdentityHelper.GetColorIdentityName(keys),
                    LinkDeckName: BuildDeckLinkName(keys),
                    ColorIdentityKeys: keys,
                    ColorSymbols: keys.Select(color => new DeckColorSymbol(color, false)).ToList(),
                    Plays: plays,
                    Wins: wins,
                    Losses: losses,
                    WinRate: plays > 0 ? wins / (double)plays : 0,
                    LastPlayed: group.Max(entry => entry.PlayedAt));
            })
            .ToList();
    }

    public ColorInclusionStat? GetMostIncludedColor(IEnumerable<PlayerScoreColorStatEntry> entries)
    {
        var favoriteColor = entries
            .SelectMany(entry => entry.ColorIdentityKeys)
            .GroupBy(color => color)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => ColorIdentityHelper.GetColorOrder(group.Key))
            .FirstOrDefault();

        if (favoriteColor is null)
        {
            return null;
        }

        return new ColorInclusionStat(
            favoriteColor.Key,
            favoriteColor.Count());
    }

    public IReadOnlyList<ColorIdentityStatRow> CalculateOpponentColorIdentityStats(IEnumerable<Play> plays, int playerId)
    {
        var groupedStats = new Dictionary<string, (IReadOnlyList<string> Keys, int Plays, int Wins, int Losses, DateTime? LastPlayed)>(StringComparer.Ordinal);

        foreach (var play in plays)
        {
            var playerScore = play.PlayerScores.FirstOrDefault(score => score.PlayerRefId == playerId);
            if (playerScore is null)
            {
                continue;
            }

            var playedAt = TryParsePlayDateTime(play.Date, out var parsedPlayedAt) ? parsedPlayedAt : (DateTime?)null;

            foreach (var opponentScore in play.PlayerScores.Where(score => score.PlayerRefId != playerId))
            {
                var opponentColorKeys = ColorIdentityHelper.GetRoleColorIdentityKeys(opponentScore.Deck);
                var opponentIdentityKey = BuildColorIdentityKey(opponentColorKeys);

                if (!groupedStats.TryGetValue(opponentIdentityKey, out var group))
                {
                    group = (opponentColorKeys, 0, 0, 0, null);
                }

                var nextPlays = group.Plays + 1;
                var nextWins = group.Wins + (playerScore.IsWinner ? 1 : 0);
                var nextLosses = group.Losses + (playerScore.IsWinner ? 0 : 1);
                var nextLastPlayed = GetLatestDate(group.LastPlayed, playedAt);

                groupedStats[opponentIdentityKey] = (group.Keys, nextPlays, nextWins, nextLosses, nextLastPlayed);
            }
        }

        return groupedStats
            .Select(group => new ColorIdentityStatRow(
                ColorIdentityName: ColorIdentityHelper.GetColorIdentityName(group.Value.Keys),
                LinkDeckName: BuildDeckLinkName(group.Value.Keys),
                ColorIdentityKeys: group.Value.Keys,
                ColorSymbols: group.Value.Keys.Select(color => new DeckColorSymbol(color, false)).ToList(),
                Plays: group.Value.Plays,
                Wins: group.Value.Wins,
                Losses: group.Value.Losses,
                WinRate: group.Value.Plays > 0 ? group.Value.Wins / (double)group.Value.Plays : 0,
                LastPlayed: group.Value.LastPlayed))
            .ToList();
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

    private static bool TryParsePlayDateTime(string playDateRaw, out DateTime playedAt)
    {
        return DateTime.TryParse(playDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out playedAt);
    }

    private static string BuildColorIdentityKey(IReadOnlyList<string> colorIdentityKeys)
    {
        return colorIdentityKeys.Count == 0 ? "Colorless" : string.Concat(colorIdentityKeys);
    }

    private static string BuildDeckLinkName(IReadOnlyList<string> colorIdentityKeys)
    {
        return colorIdentityKeys.Count == 0
            ? "Colorless"
            : string.Join('/', colorIdentityKeys.Select(ColorIdentityHelper.GetColorName));
    }

    private static DateTime? GetLatestDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left.Value >= right.Value ? left : right;
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

public sealed record PlayerScoreColorStatEntry(
    PlayerScore Score,
    IReadOnlyList<string> ColorIdentityKeys,
    DateTime? PlayedAt);

public sealed record ColorIdentityStatRow(
    string ColorIdentityName,
    string LinkDeckName,
    IReadOnlyList<string> ColorIdentityKeys,
    IReadOnlyList<DeckColorSymbol> ColorSymbols,
    int Plays,
    int Wins,
    int Losses,
    double WinRate,
    DateTime? LastPlayed);

public sealed record ColorInclusionStat(
    string ColorKey,
    int Plays);