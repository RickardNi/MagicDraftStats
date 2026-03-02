using System.Web;
using MagicDraftStats.Models;

namespace MagicDraftStats.Services
{
    public static class Utilities
    {
        public static string FormatTotalPlayTime(int totalMinutes)
        {
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            
            if (hours > 0)
                return $"{hours}h {minutes}m";
            else
                return $"{minutes}m";
        }

        public static string GetDeckUrl(string deckName)
        {
            // URL encode the deck name to handle special characters like spaces, asterisks, etc.
            var encodedDeckName = HttpUtility.UrlEncode(deckName);
            
            // Return a relative path without leading slash to work with base href
            // This ensures it works correctly on GitHub Pages with /MagicDraftStats/ base href
            return $"color/{encodedDeckName}";
        }

        public static string GetPlayerUrl(int playerId)
        {
            // Return a relative path without leading slash to work with base href
            // This ensures it works correctly on GitHub Pages with /MagicDraftStats/ base href
            return $"player/{playerId}";
        }

        public static string FormatWinRate(double winRate)
        {
            if (winRate == 0 || winRate == 1)
                return winRate.ToString("P0");
            else
                return winRate.ToString("P1");
        }

        public static string FormatNullableWinRate(double? winRate)
        {
            return winRate.HasValue ? FormatWinRate(winRate.Value) : "-";
        }

        public static string GetDeckWinRateClass(double? winRate)
        {
            return winRate.HasValue ? GetWinRateColorForDeck(winRate.Value) : "text-muted";
        }

        public static string FormatAverageDuration(double? duration, string unitLabel, bool requirePositive = false)
        {
            if (!duration.HasValue)
            {
                return "-";
            }

            if (requirePositive && duration.Value <= 0)
            {
                return "-";
            }

            return $"{duration.Value:F1} {unitLabel}";
        }

        public static string GetWinRateColorForDeck(double winRate) => winRate switch
        {
            >= 0.6 => "text-success-dark",
            >= 0.45 => "text-success",
            >= 0.40 => "text-warning",
            >= 0.35 => "text-mediocre",
            _ => "text-danger"
        };

        public static string GetWinRateColorForPlayer(double winRate) => winRate switch
        {
            >= 0.6 => "text-success-dark",
            >= 0.5 => "text-success",
            >= 0.4 => "text-warning",
            >= 0.3 => "text-mediocre",
            _ => "text-danger"
        };
    }
} 