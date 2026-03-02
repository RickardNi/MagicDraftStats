namespace MagicDraftStats.Models;

public static partial class FoundationsCardCatalog
{
    private static readonly string[] ColorTokens = ["W", "U", "B", "R", "G"];

    public static bool Contains(string cardName)
    {
        return !string.IsNullOrWhiteSpace(cardName) && MetadataByName.ContainsKey(cardName);
    }

    public static bool TryGetMetadata(string cardName, out FoundationsCardMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            metadata = default!;
            return false;
        }

        return MetadataByName.TryGetValue(cardName, out metadata!);
    }

    public static string GetDeckColorIdentityName(IEnumerable<CardEntry> cards)
    {
        var colorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var colorCardCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var orderedColors = GetOrderedDeckColors(cards, colorCounts, colorCardCounts, sortByWeight: true);

        return ColorIdentityHelper.GetColorIdentityName(orderedColors, colorCardCounts);
    }

    public static int GetManaValueFromManaCost(string? manaCost)
    {
        if (string.IsNullOrWhiteSpace(manaCost))
        {
            return 0;
        }

        var total = 0;
        var symbols = GetManaSymbols(manaCost);

        foreach (var symbol in symbols)
        {
            if (int.TryParse(symbol, out var numeric))
            {
                total += numeric;
                continue;
            }

            if (symbol.Equals("X", StringComparison.OrdinalIgnoreCase)
                || symbol.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || symbol.Equals("Z", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            total += 1;
        }

        return total;
    }

    public static IReadOnlyList<string> GetColorIdentityFromManaCost(string? manaCost)
    {
        if (string.IsNullOrWhiteSpace(manaCost))
        {
            return [];
        }

        var colors = new HashSet<string>(StringComparer.Ordinal);
        var symbols = GetManaSymbols(manaCost);

        foreach (var symbol in symbols)
        {
            var parts = symbol.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var token = part.ToUpperInvariant();
                if (ColorTokens.Contains(token, StringComparer.Ordinal))
                {
                    colors.Add(token);
                }
            }
        }

        return colors
            .OrderBy(ColorIdentityHelper.GetColorOrder)
            .ToList();
    }

    public static string GetRarityDisplayName(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => "Common",
            CardRarity.Uncommon => "Uncommon",
            CardRarity.Rare => "Rare",
            CardRarity.Mythic => "Mythic Rare",
            _ => "Unknown"
        };
    }

    public static string GetRarityShortLabel(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => "C",
            CardRarity.Uncommon => "U",
            CardRarity.Rare => "R",
            CardRarity.Mythic => "M",
            _ => "?"
        };
    }

    public static IReadOnlyList<DeckColorSymbol> GetDeckColorIdentitySymbols(IEnumerable<CardEntry> cards)
    {
        var colorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var colorCardCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var orderedColors = GetOrderedDeckColors(cards, colorCounts, colorCardCounts, sortByWeight: false);
        var hasSplashCandidates = orderedColors.Count >= 3;

        return orderedColors
            .Select(color => new DeckColorSymbol(
                color,
                hasSplashCandidates
                && colorCardCounts.TryGetValue(color, out var colorCardCount)
                && colorCardCount == 1))
            .OrderBy(symbol => symbol.IsSplash ? 1 : 0)
            .ThenBy(symbol => ColorIdentityHelper.GetColorOrder(symbol.ColorKey))
            .ToList();
    }

    private static List<string> GetOrderedDeckColors(
        IEnumerable<CardEntry> cards,
        Dictionary<string, int>? colorCounts,
        Dictionary<string, int>? colorCardCounts,
        bool sortByWeight)
    {
        colorCounts ??= new Dictionary<string, int>(StringComparer.Ordinal);
        colorCardCounts ??= new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var card in cards.Where(card => card.Count > 0))
        {
            if (!TryGetMetadata(card.Name, out var metadata))
            {
                continue;
            }

            foreach (var color in GetColorIdentityFromManaCost(metadata.ManaCost))
            {
                colorCounts[color] = colorCounts.TryGetValue(color, out var count)
                    ? count + card.Count
                    : card.Count;

                colorCardCounts[color] = colorCardCounts.TryGetValue(color, out var colorCardCount)
                    ? colorCardCount + 1
                    : 1;
            }
        }

        if (sortByWeight)
        {
            return colorCounts
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => ColorIdentityHelper.GetColorOrder(entry.Key))
                .Select(entry => entry.Key)
                .ToList();
        }

        return colorCounts
            .OrderBy(entry => ColorIdentityHelper.GetColorOrder(entry.Key))
            .Select(entry => entry.Key)
            .ToList();
    }

    private static IEnumerable<string> GetManaSymbols(string manaCost)
    {
        var index = 0;
        while (index < manaCost.Length)
        {
            var start = manaCost.IndexOf('{', index);
            if (start < 0)
            {
                yield break;
            }

            var end = manaCost.IndexOf('}', start + 1);
            if (end < 0)
            {
                yield break;
            }

            if (end > start + 1)
            {
                yield return manaCost[(start + 1)..end];
            }

            index = end + 1;
        }
    }
}

public enum CardRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Mythic = 3
}

public record FoundationsCardMetadata(string ManaCost, string[] Types, CardRarity Rarity);

public static partial class FoundationsCardCatalog
{
    public static readonly Dictionary<string, FoundationsCardMetadata> MetadataByName = new(StringComparer.Ordinal)
    {
        ["Abrade"] = new FoundationsCardMetadata("{1}{R}", [ "Instant" ], CardRarity.Uncommon),
        ["Abyssal Harvester"] = new FoundationsCardMetadata("{1}{B}{B}", [ "Creature" ], CardRarity.Rare),
        ["Adaptive Automaton"] = new FoundationsCardMetadata("{3}", [ "Artifact Creature" ], CardRarity.Rare),
        ["Aegis Turtle"] = new FoundationsCardMetadata("{U}", [ "Creature" ], CardRarity.Common),
        ["Aetherize"] = new FoundationsCardMetadata("{3}{U}", [ "Instant" ], CardRarity.Uncommon),
        ["Aggressive Mammoth"] = new FoundationsCardMetadata("{3}{G}{G}{G}", [ "Creature" ], CardRarity.Rare),
        ["Ajani, Caller of the Pride"] = new FoundationsCardMetadata("{1}{W}{W}", [ "Legendary Planeswalker" ], CardRarity.Mythic),
        ["Ajani's Pridemate"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Alesha, Who Laughs at Fate"] = new FoundationsCardMetadata("{1}{B}{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Ancestor Dragon"] = new FoundationsCardMetadata("{4}{W}{W}", [ "Creature" ], CardRarity.Rare),
        ["Angel of Finality"] = new FoundationsCardMetadata("{3}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Angelic Destiny"] = new FoundationsCardMetadata("{2}{W}{W}", [ "Enchantment" ], CardRarity.Mythic),
        ["Anthem of Champions"] = new FoundationsCardMetadata("{G}{W}", [ "Enchantment" ], CardRarity.Rare),
        ["Arahbo, the First Fang"] = new FoundationsCardMetadata("{2}{W}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Arbiter of Woe"] = new FoundationsCardMetadata("{4}{B}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Arcane Epiphany"] = new FoundationsCardMetadata("{3}{U}{U}", [ "Instant" ], CardRarity.Uncommon),
        ["Arcanis the Omnipotent"] = new FoundationsCardMetadata("{3}{U}{U}{U}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Archmage of Runes"] = new FoundationsCardMetadata("{3}{U}{U}", [ "Creature" ], CardRarity.Rare),
        ["Ashroot Animist"] = new FoundationsCardMetadata("{2}{R}{G}", [ "Creature" ], CardRarity.Rare),
        ["Aurelia, the Warleader"] = new FoundationsCardMetadata("{2}{R}{R}{W}{W}", [ "Legendary Creature" ], CardRarity.Mythic),
        ["Axgard Cavalry"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Common),
        ["Ayli, Eternal Pilgrim"] = new FoundationsCardMetadata("{W}{B}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Azorius Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Bake into a Pie"] = new FoundationsCardMetadata("{2}{B}{B}", [ "Instant" ], CardRarity.Common),
        ["Balmor, Battlemage Captain"] = new FoundationsCardMetadata("{U}{R}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Banishing Light"] = new FoundationsCardMetadata("{2}{W}", [ "Enchantment" ], CardRarity.Common),
        ["Basilisk Collar"] = new FoundationsCardMetadata("{1}", [ "Artifact" ], CardRarity.Rare),
        ["Beast-Kin Ranger"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Common),
        ["Bigfin Bouncer"] = new FoundationsCardMetadata("{3}{U}", [ "Creature" ], CardRarity.Common),
        ["Billowing Shriekmass"] = new FoundationsCardMetadata("{3}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Bite Down"] = new FoundationsCardMetadata("{1}{G}", [ "Instant" ], CardRarity.Common),
        ["Blasphemous Edict"] = new FoundationsCardMetadata("{3}{B}{B}", [ "Sorcery" ], CardRarity.Rare),
        ["Bloodfell Caves"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Bloodthirsty Conqueror"] = new FoundationsCardMetadata("{3}{B}{B}", [ "Creature" ], CardRarity.Mythic),
        ["Bloodtithe Collector"] = new FoundationsCardMetadata("{4}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Blossoming Sands"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Boros Charm"] = new FoundationsCardMetadata("{R}{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Boros Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Brazen Scourge"] = new FoundationsCardMetadata("{1}{R}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Brineborn Cutthroat"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Broken Wings"] = new FoundationsCardMetadata("{2}{G}", [ "Instant" ], CardRarity.Common),
        ["Bulk Up"] = new FoundationsCardMetadata("{1}{R}", [ "Instant" ], CardRarity.Uncommon),
        ["Burglar Rat"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Common),
        ["Burnished Hart"] = new FoundationsCardMetadata("{3}", [ "Artifact Creature" ], CardRarity.Uncommon),
        ["Burst Lightning"] = new FoundationsCardMetadata("{R}", [ "Instant" ], CardRarity.Common),
        ["Bushwhack"] = new FoundationsCardMetadata("{G}", [ "Sorcery" ], CardRarity.Common),
        ["Cancel"] = new FoundationsCardMetadata("{1}{U}{U}", [ "Instant" ], CardRarity.Common),
        ["Carnelian Orb of Dragonkind"] = new FoundationsCardMetadata("{2}{R}", [ "Artifact" ], CardRarity.Common),
        ["Cat Collector"] = new FoundationsCardMetadata("{2}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Cathar Commando"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Common),
        ["Celestial Armor"] = new FoundationsCardMetadata("{2}{W}", [ "Artifact" ], CardRarity.Rare),
        ["Chandra, Flameshaper"] = new FoundationsCardMetadata("{5}{R}{R}", [ "Legendary Planeswalker" ], CardRarity.Mythic),
        ["Charming Prince"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Rare),
        ["Chart a Course"] = new FoundationsCardMetadata("{1}{U}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Circuitous Route"] = new FoundationsCardMetadata("{3}{G}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Claws Out"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Cloudblazer"] = new FoundationsCardMetadata("{3}{W}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Confiscate"] = new FoundationsCardMetadata("{4}{U}{U}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Consuming Aberration"] = new FoundationsCardMetadata("{3}{U}{B}", [ "Creature" ], CardRarity.Rare),
        ["Corsair Captain"] = new FoundationsCardMetadata("{2}{U}", [ "Creature" ], CardRarity.Rare),
        ["Courageous Goblin"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Common),
        ["Crackling Cyclops"] = new FoundationsCardMetadata("{2}{R}", [ "Creature" ], CardRarity.Common),
        ["Crash Through"] = new FoundationsCardMetadata("{R}", [ "Sorcery" ], CardRarity.Common),
        ["Crawling Barrens"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Crossway Troublemakers"] = new FoundationsCardMetadata("{5}{B}", [ "Creature" ], CardRarity.Rare),
        ["Cryptic Caves"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Uncommon),
        ["Cultivator's Caravan"] = new FoundationsCardMetadata("{3}", [ "Artifact" ], CardRarity.Rare),
        ["Curator of Destinies"] = new FoundationsCardMetadata("{4}{U}{U}", [ "Creature" ], CardRarity.Rare),
        ["Dauntless Veteran"] = new FoundationsCardMetadata("{1}{W}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Dawnwing Marshal"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Day of Judgment"] = new FoundationsCardMetadata("{2}{W}{W}", [ "Sorcery" ], CardRarity.Rare),
        ["Dazzling Angel"] = new FoundationsCardMetadata("{2}{W}", [ "Creature" ], CardRarity.Common),
        ["Deadly Brew"] = new FoundationsCardMetadata("{B}{G}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Deadly Plot"] = new FoundationsCardMetadata("{3}{B}", [ "Instant" ], CardRarity.Uncommon),
        ["Deadly Riposte"] = new FoundationsCardMetadata("{1}{W}", [ "Instant" ], CardRarity.Common),
        ["Desecration Demon"] = new FoundationsCardMetadata("{2}{B}{B}", [ "Creature" ], CardRarity.Rare),
        ["Dimir Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Diregraf Ghoul"] = new FoundationsCardMetadata("{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Dismal Backwater"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Divine Resilience"] = new FoundationsCardMetadata("{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Dragon Fodder"] = new FoundationsCardMetadata("{1}{R}", [ "Sorcery" ], CardRarity.Common),
        ["Dragon Trainer"] = new FoundationsCardMetadata("{3}{R}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Dragonlord's Servant"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Dragonmaster Outcast"] = new FoundationsCardMetadata("{R}", [ "Creature" ], CardRarity.Mythic),
        ["Drake Hatcher"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Rare),
        ["Drakuseth, Maw of Flames"] = new FoundationsCardMetadata("{4}{R}{R}{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Dreadwing Scavenger"] = new FoundationsCardMetadata("{1}{U}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Driver of the Dead"] = new FoundationsCardMetadata("{3}{B}", [ "Creature" ], CardRarity.Common),
        ["Drogskol Reaver"] = new FoundationsCardMetadata("{5}{W}{U}", [ "Creature" ], CardRarity.Rare),
        ["Dropkick Bomber"] = new FoundationsCardMetadata("{2}{R}", [ "Creature" ], CardRarity.Rare),
        ["Druid of the Cowl"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Common),
        ["Dryad Militant"] = new FoundationsCardMetadata("{G/W}", [ "Creature" ], CardRarity.Uncommon),
        ["Dwynen, Gilt-Leaf Daen"] = new FoundationsCardMetadata("{2}{G}{G}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Dwynen's Elite"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Common),
        ["Eaten Alive"] = new FoundationsCardMetadata("{B}", [ "Sorcery" ], CardRarity.Common),
        ["Electroduplicate"] = new FoundationsCardMetadata("{2}{R}", [ "Sorcery" ], CardRarity.Rare),
        ["Elenda, Saint of Dusk"] = new FoundationsCardMetadata("{2}{W}{B}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Elvish Regrower"] = new FoundationsCardMetadata("{2}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Empyrean Eagle"] = new FoundationsCardMetadata("{1}{W}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Enigma Drake"] = new FoundationsCardMetadata("{1}{U}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Essence Scatter"] = new FoundationsCardMetadata("{1}{U}", [ "Instant" ], CardRarity.Uncommon),
        ["Etali, Primal Storm"] = new FoundationsCardMetadata("{4}{R}{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Evolving Wilds"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Exclusion Mage"] = new FoundationsCardMetadata("{2}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Exemplar of Light"] = new FoundationsCardMetadata("{2}{W}{W}", [ "Creature" ], CardRarity.Rare),
        ["Extravagant Replication"] = new FoundationsCardMetadata("{4}{U}{U}", [ "Enchantment" ], CardRarity.Rare),
        ["Faebloom Trick"] = new FoundationsCardMetadata("{2}{U}", [ "Instant" ], CardRarity.Uncommon),
        ["Fanatical Firebrand"] = new FoundationsCardMetadata("{R}", [ "Creature" ], CardRarity.Common),
        ["Feed the Swarm"] = new FoundationsCardMetadata("{1}{B}", [ "Sorcery" ], CardRarity.Common),
        ["Felidar Retreat"] = new FoundationsCardMetadata("{3}{W}", [ "Enchantment" ], CardRarity.Rare),
        ["Felidar Savior"] = new FoundationsCardMetadata("{3}{W}", [ "Creature" ], CardRarity.Common),
        ["Felling Blow"] = new FoundationsCardMetadata("{2}{G}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Fiendish Panda"] = new FoundationsCardMetadata("{2}{W}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Fierce Empath"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Common),
        ["Fiery Annihilation"] = new FoundationsCardMetadata("{2}{R}", [ "Instant" ], CardRarity.Uncommon),
        ["Finale of Revelation"] = new FoundationsCardMetadata("{X}{U}{U}", [ "Sorcery" ], CardRarity.Mythic),
        ["Firebrand Archer"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Common),
        ["Firespitter Whelp"] = new FoundationsCardMetadata("{2}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Flamewake Phoenix"] = new FoundationsCardMetadata("{1}{R}{R}", [ "Creature" ], CardRarity.Rare),
        ["Fleeting Distraction"] = new FoundationsCardMetadata("{U}", [ "Instant" ], CardRarity.Common),
        ["Fog Bank"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Forest"] = new FoundationsCardMetadata("", [ "Basic Land" ], CardRarity.Common),
        ["Fumigate"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Sorcery" ], CardRarity.Rare),
        ["Fynn, the Fangbearer"] = new FoundationsCardMetadata("{1}{G}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Garna, Bloodfist of Keld"] = new FoundationsCardMetadata("{1}{B}{R}{R}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Garruk's Uprising"] = new FoundationsCardMetadata("{2}{G}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Gate Colossus"] = new FoundationsCardMetadata("{8}", [ "Artifact Creature" ], CardRarity.Uncommon),
        ["Gatekeeper of Malakir"] = new FoundationsCardMetadata("{B}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Gateway Sneak"] = new FoundationsCardMetadata("{2}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Genesis Wave"] = new FoundationsCardMetadata("{X}{G}{G}{G}", [ "Sorcery" ], CardRarity.Rare),
        ["Ghalta, Primal Hunger"] = new FoundationsCardMetadata("{10}{G}{G}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Ghitu Lavarunner"] = new FoundationsCardMetadata("{R}", [ "Creature" ], CardRarity.Common),
        ["Giada, Font of Hope"] = new FoundationsCardMetadata("{1}{W}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Giant Growth"] = new FoundationsCardMetadata("{G}", [ "Instant" ], CardRarity.Common),
        ["Gilded Lotus"] = new FoundationsCardMetadata("{5}", [ "Artifact" ], CardRarity.Rare),
        ["Gnarlback Rhino"] = new FoundationsCardMetadata("{2}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Gnarlid Colony"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Common),
        ["Goblin Oriflamme"] = new FoundationsCardMetadata("{1}{R}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Goblin Surprise"] = new FoundationsCardMetadata("{2}{R}", [ "Instant" ], CardRarity.Common),
        ["Goldvein Pick"] = new FoundationsCardMetadata("{2}", [ "Artifact" ], CardRarity.Common),
        ["Golgari Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Good-Fortune Unicorn"] = new FoundationsCardMetadata("{1}{G}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Gorehorn Raider"] = new FoundationsCardMetadata("{4}{R}", [ "Creature" ], CardRarity.Common),
        ["Grappling Kraken"] = new FoundationsCardMetadata("{4}{U}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Grow from the Ashes"] = new FoundationsCardMetadata("{2}{G}", [ "Sorcery" ], CardRarity.Common),
        ["Gruul Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Gutless Plunderer"] = new FoundationsCardMetadata("{2}{B}", [ "Creature" ], CardRarity.Common),
        ["Guttersnipe"] = new FoundationsCardMetadata("{2}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Halana and Alena, Partners"] = new FoundationsCardMetadata("{2}{R}{G}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Harbinger of the Tides"] = new FoundationsCardMetadata("{U}{U}", [ "Creature" ], CardRarity.Rare),
        ["Healer's Hawk"] = new FoundationsCardMetadata("{W}", [ "Creature" ], CardRarity.Common),
        ["Heartfire Immolator"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Hedron Archive"] = new FoundationsCardMetadata("{4}", [ "Artifact" ], CardRarity.Uncommon),
        ["Helpful Hunter"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Common),
        ["Herald of Eternal Dawn"] = new FoundationsCardMetadata("{4}{W}{W}{W}", [ "Creature" ], CardRarity.Mythic),
        ["Heraldic Banner"] = new FoundationsCardMetadata("{3}", [ "Artifact" ], CardRarity.Uncommon),
        ["Heroes' Bane"] = new FoundationsCardMetadata("{3}{G}{G}", [ "Creature" ], CardRarity.Rare),
        ["Heroic Reinforcements"] = new FoundationsCardMetadata("{2}{R}{W}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Hero's Downfall"] = new FoundationsCardMetadata("{1}{B}{B}", [ "Instant" ], CardRarity.Uncommon),
        ["High Fae Trickster"] = new FoundationsCardMetadata("{3}{U}", [ "Creature" ], CardRarity.Rare),
        ["High-Society Hunter"] = new FoundationsCardMetadata("{3}{B}{B}", [ "Creature" ], CardRarity.Rare),
        ["Hinterland Sanctifier"] = new FoundationsCardMetadata("{W}", [ "Creature" ], CardRarity.Common),
        ["Homunculus Horde"] = new FoundationsCardMetadata("{3}{U}", [ "Creature" ], CardRarity.Rare),
        ["Hungry Ghoul"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Common),
        ["Immersturm Predator"] = new FoundationsCardMetadata("{2}{B}{R}", [ "Creature" ], CardRarity.Rare),
        ["Impact Tremors"] = new FoundationsCardMetadata("{1}{R}", [ "Enchantment" ], CardRarity.Common),
        ["Imperious Perfect"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Infestation Sage"] = new FoundationsCardMetadata("{B}", [ "Creature" ], CardRarity.Common),
        ["Inspiring Call"] = new FoundationsCardMetadata("{2}{G}", [ "Instant" ], CardRarity.Uncommon),
        ["Inspiring Overseer"] = new FoundationsCardMetadata("{2}{W}", [ "Creature" ], CardRarity.Common),
        ["Into the Roil"] = new FoundationsCardMetadata("{1}{U}", [ "Instant" ], CardRarity.Common),
        ["Involuntary Employment"] = new FoundationsCardMetadata("{3}{R}", [ "Sorcery" ], CardRarity.Common),
        ["Island"] = new FoundationsCardMetadata("", [ "Basic Land" ], CardRarity.Common),
        ["Izzet Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Jazal Goldmane"] = new FoundationsCardMetadata("{2}{W}{W}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Joraga Invocation"] = new FoundationsCardMetadata("{4}{G}{G}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Joust Through"] = new FoundationsCardMetadata("{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Jungle Hollow"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Kaito, Cunning Infiltrator"] = new FoundationsCardMetadata("{1}{U}{U}", [ "Legendary Planeswalker" ], CardRarity.Mythic),
        ["Kellan, Planar Trailblazer"] = new FoundationsCardMetadata("{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Kiora, the Rising Tide"] = new FoundationsCardMetadata("{2}{U}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Kitesail Corsair"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Common),
        ["Koma, World-Eater"] = new FoundationsCardMetadata("{3}{G}{G}{U}{U}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Kykar, Zephyr Awakener"] = new FoundationsCardMetadata("{2}{W}{U}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Lathliss, Dragon Queen"] = new FoundationsCardMetadata("{4}{R}{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Lathril, Blade of the Elves"] = new FoundationsCardMetadata("{2}{B}{G}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Leonin Vanguard"] = new FoundationsCardMetadata("{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Leyline Axe"] = new FoundationsCardMetadata("{4}", [ "Artifact" ], CardRarity.Rare),
        ["Liliana, Dreadhorde General"] = new FoundationsCardMetadata("{4}{B}{B}", [ "Legendary Planeswalker" ], CardRarity.Mythic),
        ["Llanowar Elves"] = new FoundationsCardMetadata("{G}", [ "Creature" ], CardRarity.Common),
        ["Loot, Exuberant Explorer"] = new FoundationsCardMetadata("{2}{G}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Luminous Rebuke"] = new FoundationsCardMetadata("{4}{W}", [ "Instant" ], CardRarity.Common),
        ["Lyra Dawnbringer"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Legendary Creature" ], CardRarity.Mythic),
        ["Macabre Waltz"] = new FoundationsCardMetadata("{1}{B}", [ "Sorcery" ], CardRarity.Common),
        ["Maelstrom Pulse"] = new FoundationsCardMetadata("{1}{B}{G}", [ "Sorcery" ], CardRarity.Rare),
        ["Make a Stand"] = new FoundationsCardMetadata("{2}{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Make Your Move"] = new FoundationsCardMetadata("{2}{W}", [ "Instant" ], CardRarity.Common),
        ["Marauding Blight-Priest"] = new FoundationsCardMetadata("{2}{B}", [ "Creature" ], CardRarity.Common),
        ["Massacre Wurm"] = new FoundationsCardMetadata("{3}{B}{B}{B}", [ "Creature" ], CardRarity.Mythic),
        ["Mazemind Tome"] = new FoundationsCardMetadata("{2}", [ "Artifact" ], CardRarity.Rare),
        ["Mentor of the Meek"] = new FoundationsCardMetadata("{2}{W}", [ "Creature" ], CardRarity.Rare),
        ["Meteor Golem"] = new FoundationsCardMetadata("{7}", [ "Artifact Creature" ], CardRarity.Uncommon),
        ["Micromancer"] = new FoundationsCardMetadata("{3}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Midnight Reaper"] = new FoundationsCardMetadata("{2}{B}", [ "Creature" ], CardRarity.Rare),
        ["Mild-Mannered Librarian"] = new FoundationsCardMetadata("{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Mischievous Mystic"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Moment of Craving"] = new FoundationsCardMetadata("{1}{B}", [ "Instant" ], CardRarity.Common),
        ["Mortify"] = new FoundationsCardMetadata("{1}{W}{B}", [ "Instant" ], CardRarity.Uncommon),
        ["Mossborn Hydra"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Rare),
        ["Mountain"] = new FoundationsCardMetadata("", [ "Basic Land" ], CardRarity.Common),
        ["Muldrotha, the Gravetide"] = new FoundationsCardMetadata("{3}{B}{G}{U}", [ "Legendary Creature" ], CardRarity.Mythic),
        ["Mystic Archaeologist"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Rare),
        ["Mystical Teachings"] = new FoundationsCardMetadata("{3}{U}", [ "Instant" ], CardRarity.Uncommon),
        ["Needletooth Pack"] = new FoundationsCardMetadata("{3}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Nessian Hornbeetle"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Nine-Lives Familiar"] = new FoundationsCardMetadata("{1}{B}{B}", [ "Creature" ], CardRarity.Rare),
        ["Nullpriest of Oblivion"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Rare),
        ["Obliterating Bolt"] = new FoundationsCardMetadata("{1}{R}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Offer Immortality"] = new FoundationsCardMetadata("{1}{B}", [ "Instant" ], CardRarity.Common),
        ["Opt"] = new FoundationsCardMetadata("{U}", [ "Instant" ], CardRarity.Common),
        ["Ordeal of Nylea"] = new FoundationsCardMetadata("{1}{G}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Orzhov Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Overrun"] = new FoundationsCardMetadata("{2}{G}{G}{G}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Ovika, Enigma Goliath"] = new FoundationsCardMetadata("{5}{U}{R}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Pacifism"] = new FoundationsCardMetadata("{1}{W}", [ "Enchantment" ], CardRarity.Common),
        ["Pelakka Wurm"] = new FoundationsCardMetadata("{4}{G}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Perforating Artist"] = new FoundationsCardMetadata("{1}{B}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Phyrexian Arena"] = new FoundationsCardMetadata("{1}{B}{B}", [ "Enchantment" ], CardRarity.Rare),
        ["Pilfer"] = new FoundationsCardMetadata("{1}{B}", [ "Sorcery" ], CardRarity.Common),
        ["Plains"] = new FoundationsCardMetadata("", [ "Basic Land" ], CardRarity.Common),
        ["Prayer of Binding"] = new FoundationsCardMetadata("{3}{W}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Predator Ooze"] = new FoundationsCardMetadata("{G}{G}{G}", [ "Creature" ], CardRarity.Rare),
        ["Prideful Parent"] = new FoundationsCardMetadata("{2}{W}", [ "Creature" ], CardRarity.Common),
        ["Primal Might"] = new FoundationsCardMetadata("{X}{G}", [ "Sorcery" ], CardRarity.Rare),
        ["Prime Speaker Zegana"] = new FoundationsCardMetadata("{2}{G}{G}{U}{U}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Primeval Bounty"] = new FoundationsCardMetadata("{5}{G}", [ "Enchantment" ], CardRarity.Mythic),
        ["Quick Study"] = new FoundationsCardMetadata("{2}{U}", [ "Instant" ], CardRarity.Common),
        ["Quilled Greatwurm"] = new FoundationsCardMetadata("{4}{G}{G}", [ "Creature" ], CardRarity.Mythic),
        ["Rakdos Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Ramos, Dragon Engine"] = new FoundationsCardMetadata("{6}", [ "Legendary Artifact Creature" ], CardRarity.Mythic),
        ["Rampaging Baloths"] = new FoundationsCardMetadata("{4}{G}{G}", [ "Creature" ], CardRarity.Rare),
        ["Rapacious Dragon"] = new FoundationsCardMetadata("{4}{R}", [ "Creature" ], CardRarity.Common),
        ["Ravenous Amulet"] = new FoundationsCardMetadata("{2}", [ "Artifact" ], CardRarity.Uncommon),
        ["Reassembling Skeleton"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Redcap Gutter-Dweller"] = new FoundationsCardMetadata("{2}{R}{R}", [ "Creature" ], CardRarity.Rare),
        ["Refute"] = new FoundationsCardMetadata("{1}{U}{U}", [ "Instant" ], CardRarity.Common),
        ["Regal Caracal"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Creature" ], CardRarity.Rare),
        ["Resolute Reinforcements"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Revenge of the Rats"] = new FoundationsCardMetadata("{2}{B}{B}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Rise of the Dark Realms"] = new FoundationsCardMetadata("{7}{B}{B}", [ "Sorcery" ], CardRarity.Mythic),
        ["Rite of Replication"] = new FoundationsCardMetadata("{2}{U}{U}", [ "Sorcery" ], CardRarity.Rare),
        ["Rite of the Dragoncaller"] = new FoundationsCardMetadata("{4}{R}{R}", [ "Enchantment" ], CardRarity.Mythic),
        ["River's Rebuke"] = new FoundationsCardMetadata("{4}{U}{U}", [ "Sorcery" ], CardRarity.Rare),
        ["Ruby, Daring Tracker"] = new FoundationsCardMetadata("{R}{G}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Rugged Highlands"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Run Away Together"] = new FoundationsCardMetadata("{1}{U}", [ "Instant" ], CardRarity.Common),
        ["Rune-Scarred Demon"] = new FoundationsCardMetadata("{5}{B}{B}", [ "Creature" ], CardRarity.Rare),
        ["Rune-Sealed Wall"] = new FoundationsCardMetadata("{2}{U}", [ "Artifact Creature" ], CardRarity.Uncommon),
        ["Sanguine Indulgence"] = new FoundationsCardMetadata("{3}{B}", [ "Sorcery" ], CardRarity.Common),
        ["Sanguine Syphoner"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Common),
        ["Savage Ventmaw"] = new FoundationsCardMetadata("{4}{R}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Scavenging Ooze"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Rare),
        ["Scorching Dragonfire"] = new FoundationsCardMetadata("{1}{R}", [ "Instant" ], CardRarity.Common),
        ["Scoured Barrens"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Scrawling Crawler"] = new FoundationsCardMetadata("{3}", [ "Artifact Creature" ], CardRarity.Rare),
        ["Searslicer Goblin"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Rare),
        ["Seeker's Folly"] = new FoundationsCardMetadata("{2}{B}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Seize the Spoils"] = new FoundationsCardMetadata("{2}{R}", [ "Sorcery" ], CardRarity.Common),
        ["Selesnya Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Serra Angel"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Shipwreck Dowser"] = new FoundationsCardMetadata("{3}{U}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Simic Guildgate"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Sire of Seven Deaths"] = new FoundationsCardMetadata("{7}", [ "Creature" ], CardRarity.Mythic),
        ["Skyknight Squire"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Rare),
        ["Skyship Buccaneer"] = new FoundationsCardMetadata("{3}{U}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Slagstorm"] = new FoundationsCardMetadata("{1}{R}{R}", [ "Sorcery" ], CardRarity.Rare),
        ["Slumbering Cerberus"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Snakeskin Veil"] = new FoundationsCardMetadata("{G}", [ "Instant" ], CardRarity.Uncommon),
        ["Solemn Simulacrum"] = new FoundationsCardMetadata("{4}", [ "Artifact Creature" ], CardRarity.Rare),
        ["Sower of Chaos"] = new FoundationsCardMetadata("{3}{R}", [ "Creature" ], CardRarity.Common),
        ["Spectral Sailor"] = new FoundationsCardMetadata("{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Sphinx of Forgotten Lore"] = new FoundationsCardMetadata("{2}{U}{U}", [ "Creature" ], CardRarity.Mythic),
        ["Spinner of Souls"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Rare),
        ["Springbloom Druid"] = new FoundationsCardMetadata("{2}{G}", [ "Creature" ], CardRarity.Common),
        ["Stab"] = new FoundationsCardMetadata("{B}", [ "Instant" ], CardRarity.Common),
        ["Stasis Snare"] = new FoundationsCardMetadata("{1}{W}{W}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Steel Hellkite"] = new FoundationsCardMetadata("{6}", [ "Artifact Creature" ], CardRarity.Rare),
        ["Storm Fleet Spy"] = new FoundationsCardMetadata("{2}{U}", [ "Creature" ], CardRarity.Uncommon),
        ["Strix Lookout"] = new FoundationsCardMetadata("{1}{U}", [ "Creature" ], CardRarity.Common),
        ["Stroke of Midnight"] = new FoundationsCardMetadata("{2}{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Stromkirk Noble"] = new FoundationsCardMetadata("{R}", [ "Creature" ], CardRarity.Rare),
        ["Strongbox Raider"] = new FoundationsCardMetadata("{2}{R}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Sun-Blessed Healer"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Surrak, the Hunt Caller"] = new FoundationsCardMetadata("{2}{G}{G}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Swamp"] = new FoundationsCardMetadata("", [ "Basic Land" ], CardRarity.Common),
        ["Swiftblade Vindicator"] = new FoundationsCardMetadata("{R}{W}", [ "Creature" ], CardRarity.Rare),
        ["Swiftfoot Boots"] = new FoundationsCardMetadata("{2}", [ "Artifact" ], CardRarity.Uncommon),
        ["Swiftwater Cliffs"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Sylvan Scavenging"] = new FoundationsCardMetadata("{1}{G}{G}", [ "Enchantment" ], CardRarity.Rare),
        ["Tatyova, Benthic Druid"] = new FoundationsCardMetadata("{3}{G}{U}", [ "Legendary Creature" ], CardRarity.Uncommon),
        ["Taurean Mauler"] = new FoundationsCardMetadata("{2}{R}", [ "Creature" ], CardRarity.Rare),
        ["Temple of Abandon"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Deceit"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Enlightenment"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Epiphany"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Malady"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Malice"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Mystery"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Plenty"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Silence"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Temple of Triumph"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Rare),
        ["Terror of Mount Velus"] = new FoundationsCardMetadata("{5}{R}{R}", [ "Creature" ], CardRarity.Rare),
        ["Think Twice"] = new FoundationsCardMetadata("{1}{U}", [ "Instant" ], CardRarity.Common),
        ["Thornweald Archer"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Common),
        ["Thornwood Falls"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Thousand-Year Storm"] = new FoundationsCardMetadata("{4}{U}{R}", [ "Enchantment" ], CardRarity.Rare),
        ["Thrashing Brontodon"] = new FoundationsCardMetadata("{1}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Three Tree Mascot"] = new FoundationsCardMetadata("{2}", [ "Artifact Creature" ], CardRarity.Common),
        ["Thrill of Possibility"] = new FoundationsCardMetadata("{1}{R}", [ "Instant" ], CardRarity.Common),
        ["Tinybones, Bauble Burglar"] = new FoundationsCardMetadata("{1}{B}", [ "Legendary Creature" ], CardRarity.Rare),
        ["Tolarian Terror"] = new FoundationsCardMetadata("{6}{U}", [ "Creature" ], CardRarity.Common),
        ["Tragic Banshee"] = new FoundationsCardMetadata("{4}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Tranquil Cove"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Treetop Snarespinner"] = new FoundationsCardMetadata("{3}{G}", [ "Creature" ], CardRarity.Common),
        ["Tribute to Hunger"] = new FoundationsCardMetadata("{2}{B}", [ "Instant" ], CardRarity.Uncommon),
        ["Twinblade Paladin"] = new FoundationsCardMetadata("{3}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Twinflame Tyrant"] = new FoundationsCardMetadata("{3}{R}{R}", [ "Creature" ], CardRarity.Mythic),
        ["Uncharted Voyage"] = new FoundationsCardMetadata("{3}{U}", [ "Instant" ], CardRarity.Common),
        ["Unsummon"] = new FoundationsCardMetadata("{U}", [ "Instant" ], CardRarity.Common),
        ["Valkyrie's Call"] = new FoundationsCardMetadata("{3}{W}{W}", [ "Enchantment" ], CardRarity.Mythic),
        ["Valorous Stance"] = new FoundationsCardMetadata("{1}{W}", [ "Instant" ], CardRarity.Uncommon),
        ["Vampire Gourmand"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Vampire Interloper"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Common),
        ["Vampire Nighthawk"] = new FoundationsCardMetadata("{1}{B}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Vampiric Rites"] = new FoundationsCardMetadata("{B}", [ "Enchantment" ], CardRarity.Uncommon),
        ["Vanguard Seraph"] = new FoundationsCardMetadata("{3}{W}", [ "Creature" ], CardRarity.Common),
        ["Vengeful Bloodwitch"] = new FoundationsCardMetadata("{1}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Venom Connoisseur"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Viashino Pyromancer"] = new FoundationsCardMetadata("{1}{R}", [ "Creature" ], CardRarity.Common),
        ["Vile Entomber"] = new FoundationsCardMetadata("{2}{B}{B}", [ "Creature" ], CardRarity.Uncommon),
        ["Vivien Reid"] = new FoundationsCardMetadata("{3}{G}{G}", [ "Legendary Planeswalker" ], CardRarity.Mythic),
        ["Vizier of the Menagerie"] = new FoundationsCardMetadata("{3}{G}", [ "Creature" ], CardRarity.Mythic),
        ["Volley Veteran"] = new FoundationsCardMetadata("{3}{R}", [ "Creature" ], CardRarity.Uncommon),
        ["Voracious Greatshark"] = new FoundationsCardMetadata("{3}{U}{U}", [ "Creature" ], CardRarity.Rare),
        ["Wardens of the Cycle"] = new FoundationsCardMetadata("{1}{B}{G}{G}", [ "Creature" ], CardRarity.Uncommon),
        ["Wildborn Preserver"] = new FoundationsCardMetadata("{1}{G}", [ "Creature" ], CardRarity.Rare),
        ["Wilt-Leaf Liege"] = new FoundationsCardMetadata("{1}{G/W}{G/W}{G/W}", [ "Creature" ], CardRarity.Rare),
        ["Wind-Scarred Crag"] = new FoundationsCardMetadata("", [ "Land" ], CardRarity.Common),
        ["Witness Protection"] = new FoundationsCardMetadata("{U}", [ "Enchantment" ], CardRarity.Common),
        ["Youthful Valkyrie"] = new FoundationsCardMetadata("{1}{W}", [ "Creature" ], CardRarity.Uncommon),
        ["Zimone, Paradox Sculptor"] = new FoundationsCardMetadata("{2}{G}{U}", [ "Legendary Creature" ], CardRarity.Mythic),
        ["Zombify"] = new FoundationsCardMetadata("{3}{B}", [ "Sorcery" ], CardRarity.Uncommon),
        ["Zul Ashur, Lich Lord"] = new FoundationsCardMetadata("{1}{B}", [ "Legendary Creature" ], CardRarity.Rare),
    };
}

