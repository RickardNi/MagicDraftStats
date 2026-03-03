namespace MagicDraftStats.Models;

public enum BadgeType
{
    None,
    MajorTrophy,
    MinorTrophy
}

public static class Badge
{
    public static BadgeType ForWinner(int? playerCount) => playerCount switch
    {
        >= 4 => BadgeType.MajorTrophy,
        >= 2 => BadgeType.MinorTrophy,
        _ => BadgeType.None
    };
}
