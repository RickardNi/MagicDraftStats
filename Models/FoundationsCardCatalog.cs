namespace MagicDraftStats.Models;

public static partial class FoundationsCardCatalog
{
    public static IEnumerable<string> CardNames => MetadataByName.Keys;

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

        foreach (var card in cards.Where(card => card.Count > 0))
        {
            if (!TryGetMetadata(card.Name, out var metadata))
            {
                continue;
            }

            foreach (var color in metadata.ColorIdentity.Distinct(StringComparer.Ordinal))
            {
                colorCounts[color] = colorCounts.TryGetValue(color, out var count)
                    ? count + card.Count
                    : card.Count;

                colorCardCounts[color] = colorCardCounts.TryGetValue(color, out var colorCardCount)
                    ? colorCardCount + 1
                    : 1;
            }
        }

        if (colorCounts.Count == 0)
        {
            return "Colorless";
        }

        var orderedColors = colorCounts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => GetColorOrder(entry.Key))
            .Select(entry => entry.Key)
            .ToList();

        if (orderedColors.Count == 1)
        {
            return $"Mono {GetColorName(orderedColors[0])}";
        }

        if (orderedColors.Count == 2)
        {
            return GetTwoColorName(orderedColors[0], orderedColors[1]);
        }

        if (orderedColors.Count == 3)
        {
            var splashColor = orderedColors[2];
            if (colorCardCounts.TryGetValue(splashColor, out var splashCards) && splashCards == 1)
            {
                return $"{GetTwoColorName(orderedColors[0], orderedColors[1])}+";
            }
        }

        return string.Concat(orderedColors);
    }

    private static int GetColorOrder(string color) => color switch
    {
        "W" => 0,
        "U" => 1,
        "B" => 2,
        "R" => 3,
        "G" => 4,
        _ => 99
    };

    private static string GetColorName(string color) => color switch
    {
        "W" => "White",
        "U" => "Blue",
        "B" => "Black",
        "R" => "Red",
        "G" => "Green",
        _ => "Colorless"
    };

    private static string GetTwoColorName(string colorA, string colorB)
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

    private static bool HasType(IEnumerable<string> types, string typeName)
    {
        return types.Any(type =>
            type.Equals(typeName, StringComparison.OrdinalIgnoreCase)
            || type.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => token.Equals(typeName, StringComparison.OrdinalIgnoreCase)));
    }
}

public record FoundationsCardMetadata(int ManaValue, string[] Types, string[] ColorIdentity);

public static partial class FoundationsCardCatalog
{
    public static readonly Dictionary<string, FoundationsCardMetadata> MetadataByName = new(StringComparer.Ordinal)
    {
        ["Abrade"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "R" ]),
        ["Abyssal Harvester"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Adaptive Automaton"] = new FoundationsCardMetadata(3, [ "Artifact Creature" ], []),
        ["Aegis Turtle"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "U" ]),
        ["Aetherize"] = new FoundationsCardMetadata(4, [ "Instant" ], [ "U" ]),
        ["Aggressive Mammoth"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "G" ]),
        ["Ajani, Caller of the Pride"] = new FoundationsCardMetadata(3, [ "Legendary Planeswalker" ], [ "W" ]),
        ["Ajani's Pridemate"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Alesha, Who Laughs at Fate"] = new FoundationsCardMetadata(3, [ "Legendary Creature" ], [ "B", "R" ]),
        ["Ancestor Dragon"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "W" ]),
        ["Angel of Finality"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "W" ]),
        ["Angelic Destiny"] = new FoundationsCardMetadata(4, [ "Enchantment" ], [ "W" ]),
        ["Anthem of Champions"] = new FoundationsCardMetadata(2, [ "Enchantment" ], [ "G", "W" ]),
        ["Arahbo, the First Fang"] = new FoundationsCardMetadata(3, [ "Legendary Creature" ], [ "W" ]),
        ["Arbiter of Woe"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "B" ]),
        ["Arcane Epiphany"] = new FoundationsCardMetadata(5, [ "Instant" ], [ "U" ]),
        ["Arcanis the Omnipotent"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "U" ]),
        ["Archmage of Runes"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "U" ]),
        ["Ashroot Animist"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G", "R" ]),
        ["Aurelia, the Warleader"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "R", "W" ]),
        ["Axgard Cavalry"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Ayli, Eternal Pilgrim"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "B", "W" ]),
        ["Azorius Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Bake into a Pie"] = new FoundationsCardMetadata(4, [ "Instant" ], [ "B" ]),
        ["Balmor, Battlemage Captain"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "R", "U" ]),
        ["Banishing Light"] = new FoundationsCardMetadata(3, [ "Enchantment" ], [ "W" ]),
        ["Basilisk Collar"] = new FoundationsCardMetadata(1, [ "Artifact" ], []),
        ["Beast-Kin Ranger"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Bigfin Bouncer"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "U" ]),
        ["Billowing Shriekmass"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B" ]),
        ["Bite Down"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "G" ]),
        ["Blasphemous Edict"] = new FoundationsCardMetadata(5, [ "Sorcery" ], [ "B" ]),
        ["Bloodfell Caves"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Bloodthirsty Conqueror"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "B" ]),
        ["Bloodtithe Collector"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "B" ]),
        ["Blossoming Sands"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Boros Charm"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "R", "W" ]),
        ["Boros Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Brazen Scourge"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Brineborn Cutthroat"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Broken Wings"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "G" ]),
        ["Bulk Up"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "R" ]),
        ["Burglar Rat"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Burnished Hart"] = new FoundationsCardMetadata(3, [ "Artifact Creature" ], []),
        ["Burst Lightning"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "R" ]),
        ["Bushwhack"] = new FoundationsCardMetadata(1, [ "Sorcery" ], [ "G" ]),
        ["Cancel"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "U" ]),
        ["Carnelian Orb of Dragonkind"] = new FoundationsCardMetadata(3, [ "Artifact" ], [ "R" ]),
        ["Cat Collector"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Cathar Commando"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Celestial Armor"] = new FoundationsCardMetadata(3, [ "Artifact" ], [ "W" ]),
        ["Chandra, Flameshaper"] = new FoundationsCardMetadata(7, [ "Legendary Planeswalker" ], [ "R" ]),
        ["Charming Prince"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Chart a Course"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "U" ]),
        ["Circuitous Route"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "G" ]),
        ["Claws Out"] = new FoundationsCardMetadata(5, [ "Instant" ], [ "W" ]),
        ["Cloudblazer"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "U", "W" ]),
        ["Confiscate"] = new FoundationsCardMetadata(6, [ "Enchantment" ], [ "U" ]),
        ["Consuming Aberration"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "B", "U" ]),
        ["Corsair Captain"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "U" ]),
        ["Courageous Goblin"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Crackling Cyclops"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Crash Through"] = new FoundationsCardMetadata(1, [ "Sorcery" ], [ "R" ]),
        ["Crawling Barrens"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Crossway Troublemakers"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "B" ]),
        ["Cryptic Caves"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Cultivator's Caravan"] = new FoundationsCardMetadata(3, [ "Artifact" ], []),
        ["Curator of Destinies"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "U" ]),
        ["Dauntless Veteran"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Dawnwing Marshal"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Day of Judgment"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "W" ]),
        ["Dazzling Angel"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Deadly Brew"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "B", "G" ]),
        ["Deadly Plot"] = new FoundationsCardMetadata(4, [ "Instant" ], [ "B" ]),
        ["Deadly Riposte"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "W" ]),
        ["Desecration Demon"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B" ]),
        ["Dimir Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Diregraf Ghoul"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "B" ]),
        ["Dismal Backwater"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Divine Resilience"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "W" ]),
        ["Dragon Fodder"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "R" ]),
        ["Dragon Trainer"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "R" ]),
        ["Dragonlord's Servant"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Dragonmaster Outcast"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "R" ]),
        ["Drake Hatcher"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Drakuseth, Maw of Flames"] = new FoundationsCardMetadata(7, [ "Legendary Creature" ], [ "R" ]),
        ["Dreadwing Scavenger"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B", "U" ]),
        ["Driver of the Dead"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B" ]),
        ["Drogskol Reaver"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "U", "W" ]),
        ["Dropkick Bomber"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Druid of the Cowl"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Dryad Militant"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "G", "W" ]),
        ["Dwynen, Gilt-Leaf Daen"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "G" ]),
        ["Dwynen's Elite"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Eaten Alive"] = new FoundationsCardMetadata(1, [ "Sorcery" ], [ "B" ]),
        ["Electroduplicate"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "R" ]),
        ["Elenda, Saint of Dusk"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "B", "W" ]),
        ["Elvish Regrower"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G" ]),
        ["Empyrean Eagle"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "U", "W" ]),
        ["Enigma Drake"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R", "U" ]),
        ["Essence Scatter"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "U" ]),
        ["Etali, Primal Storm"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "R" ]),
        ["Evolving Wilds"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Exclusion Mage"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "U" ]),
        ["Exemplar of Light"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "W" ]),
        ["Extravagant Replication"] = new FoundationsCardMetadata(6, [ "Enchantment" ], [ "U" ]),
        ["Faebloom Trick"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "U" ]),
        ["Fanatical Firebrand"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "R" ]),
        ["Feed the Swarm"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "B" ]),
        ["Felidar Retreat"] = new FoundationsCardMetadata(4, [ "Enchantment" ], [ "W" ]),
        ["Felidar Savior"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "W" ]),
        ["Felling Blow"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "G" ]),
        ["Fiendish Panda"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B", "W" ]),
        ["Fierce Empath"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Fiery Annihilation"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "R" ]),
        ["Finale of Revelation"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "U" ]),
        ["Firebrand Archer"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Firespitter Whelp"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Flamewake Phoenix"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Fleeting Distraction"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "U" ]),
        ["Fog Bank"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Forest"] = new FoundationsCardMetadata(0, [ "Basic Land" ], []),
        ["Fumigate"] = new FoundationsCardMetadata(5, [ "Sorcery" ], [ "W" ]),
        ["Fynn, the Fangbearer"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "G" ]),
        ["Garna, Bloodfist of Keld"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "B", "R" ]),
        ["Garruk's Uprising"] = new FoundationsCardMetadata(3, [ "Enchantment" ], [ "G" ]),
        ["Gate Colossus"] = new FoundationsCardMetadata(8, [ "Artifact Creature" ], []),
        ["Gatekeeper of Malakir"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Gateway Sneak"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "U" ]),
        ["Genesis Wave"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "G" ]),
        ["Ghalta, Primal Hunger"] = new FoundationsCardMetadata(12, [ "Legendary Creature" ], [ "G" ]),
        ["Ghitu Lavarunner"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "R" ]),
        ["Giada, Font of Hope"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "W" ]),
        ["Giant Growth"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "G" ]),
        ["Gilded Lotus"] = new FoundationsCardMetadata(5, [ "Artifact" ], []),
        ["Gnarlback Rhino"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G" ]),
        ["Gnarlid Colony"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Goblin Oriflamme"] = new FoundationsCardMetadata(2, [ "Enchantment" ], [ "R" ]),
        ["Goblin Surprise"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "R" ]),
        ["Goldvein Pick"] = new FoundationsCardMetadata(2, [ "Artifact" ], []),
        ["Golgari Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Good-Fortune Unicorn"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G", "W" ]),
        ["Gorehorn Raider"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "R" ]),
        ["Grappling Kraken"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "U" ]),
        ["Grow from the Ashes"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "G" ]),
        ["Gruul Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Gutless Plunderer"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Guttersnipe"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Halana and Alena, Partners"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "G", "R" ]),
        ["Harbinger of the Tides"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Healer's Hawk"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "W" ]),
        ["Heartfire Immolator"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Hedron Archive"] = new FoundationsCardMetadata(4, [ "Artifact" ], []),
        ["Helpful Hunter"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Herald of Eternal Dawn"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "W" ]),
        ["Heraldic Banner"] = new FoundationsCardMetadata(3, [ "Artifact" ], []),
        ["Heroes' Bane"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "G" ]),
        ["Heroic Reinforcements"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "R", "W" ]),
        ["Hero's Downfall"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "B" ]),
        ["High Fae Trickster"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "U" ]),
        ["High-Society Hunter"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "B" ]),
        ["Hinterland Sanctifier"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "W" ]),
        ["Homunculus Horde"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "U" ]),
        ["Hungry Ghoul"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Immersturm Predator"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B", "R" ]),
        ["Impact Tremors"] = new FoundationsCardMetadata(2, [ "Enchantment" ], [ "R" ]),
        ["Imperious Perfect"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Infestation Sage"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "B" ]),
        ["Inspiring Call"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "G" ]),
        ["Inspiring Overseer"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Into the Roil"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "U" ]),
        ["Involuntary Employment"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "R" ]),
        ["Island"] = new FoundationsCardMetadata(0, [ "Basic Land" ], []),
        ["Izzet Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Jazal Goldmane"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "W" ]),
        ["Joraga Invocation"] = new FoundationsCardMetadata(6, [ "Sorcery" ], [ "G" ]),
        ["Joust Through"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "W" ]),
        ["Jungle Hollow"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Kaito, Cunning Infiltrator"] = new FoundationsCardMetadata(3, [ "Legendary Planeswalker" ], [ "U" ]),
        ["Kellan, Planar Trailblazer"] = new FoundationsCardMetadata(1, [ "Legendary Creature" ], [ "R" ]),
        ["Kiora, the Rising Tide"] = new FoundationsCardMetadata(3, [ "Legendary Creature" ], [ "U" ]),
        ["Kitesail Corsair"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Koma, World-Eater"] = new FoundationsCardMetadata(7, [ "Legendary Creature" ], [ "G", "U" ]),
        ["Kykar, Zephyr Awakener"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "U", "W" ]),
        ["Lathliss, Dragon Queen"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "R" ]),
        ["Lathril, Blade of the Elves"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "B", "G" ]),
        ["Leonin Vanguard"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "W" ]),
        ["Leyline Axe"] = new FoundationsCardMetadata(4, [ "Artifact" ], []),
        ["Liliana, Dreadhorde General"] = new FoundationsCardMetadata(6, [ "Legendary Planeswalker" ], [ "B" ]),
        ["Llanowar Elves"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "G" ]),
        ["Loot, Exuberant Explorer"] = new FoundationsCardMetadata(3, [ "Legendary Creature" ], [ "G" ]),
        ["Luminous Rebuke"] = new FoundationsCardMetadata(5, [ "Instant" ], [ "W" ]),
        ["Lyra Dawnbringer"] = new FoundationsCardMetadata(5, [ "Legendary Creature" ], [ "W" ]),
        ["Macabre Waltz"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "B" ]),
        ["Maelstrom Pulse"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "B", "G" ]),
        ["Make a Stand"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "W" ]),
        ["Make Your Move"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "W" ]),
        ["Marauding Blight-Priest"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Massacre Wurm"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "B" ]),
        ["Mazemind Tome"] = new FoundationsCardMetadata(2, [ "Artifact" ], []),
        ["Mentor of the Meek"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Meteor Golem"] = new FoundationsCardMetadata(7, [ "Artifact Creature" ], []),
        ["Micromancer"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "U" ]),
        ["Midnight Reaper"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Mild-Mannered Librarian"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "G" ]),
        ["Mischievous Mystic"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Moment of Craving"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "B" ]),
        ["Mortify"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "B", "W" ]),
        ["Mossborn Hydra"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Mountain"] = new FoundationsCardMetadata(0, [ "Basic Land" ], []),
        ["Muldrotha, the Gravetide"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "B", "G", "U" ]),
        ["Mystic Archaeologist"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Mystical Teachings"] = new FoundationsCardMetadata(4, [ "Instant" ], [ "B", "U" ]),
        ["Needletooth Pack"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "G" ]),
        ["Nessian Hornbeetle"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Nine-Lives Familiar"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Nullpriest of Oblivion"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Obliterating Bolt"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "R" ]),
        ["Offer Immortality"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "B" ]),
        ["Opt"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "U" ]),
        ["Ordeal of Nylea"] = new FoundationsCardMetadata(2, [ "Enchantment" ], [ "G" ]),
        ["Orzhov Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Overrun"] = new FoundationsCardMetadata(5, [ "Sorcery" ], [ "G" ]),
        ["Ovika, Enigma Goliath"] = new FoundationsCardMetadata(7, [ "Legendary Creature" ], [ "R", "U" ]),
        ["Pacifism"] = new FoundationsCardMetadata(2, [ "Enchantment" ], [ "W" ]),
        ["Pelakka Wurm"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "G" ]),
        ["Perforating Artist"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B", "R" ]),
        ["Phyrexian Arena"] = new FoundationsCardMetadata(3, [ "Enchantment" ], [ "B" ]),
        ["Pilfer"] = new FoundationsCardMetadata(2, [ "Sorcery" ], [ "B" ]),
        ["Plains"] = new FoundationsCardMetadata(0, [ "Basic Land" ], []),
        ["Prayer of Binding"] = new FoundationsCardMetadata(4, [ "Enchantment" ], [ "W" ]),
        ["Predator Ooze"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Prideful Parent"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "W" ]),
        ["Primal Might"] = new FoundationsCardMetadata(1, [ "Sorcery" ], [ "G" ]),
        ["Prime Speaker Zegana"] = new FoundationsCardMetadata(6, [ "Legendary Creature" ], [ "G", "U" ]),
        ["Primeval Bounty"] = new FoundationsCardMetadata(6, [ "Enchantment" ], [ "G" ]),
        ["Quick Study"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "U" ]),
        ["Quilled Greatwurm"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "G" ]),
        ["Rakdos Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Ramos, Dragon Engine"] = new FoundationsCardMetadata(6, [ "Legendary Artifact Creature" ], []),
        ["Rampaging Baloths"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "G" ]),
        ["Rapacious Dragon"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "R" ]),
        ["Ravenous Amulet"] = new FoundationsCardMetadata(2, [ "Artifact" ], []),
        ["Reassembling Skeleton"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Redcap Gutter-Dweller"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "R" ]),
        ["Refute"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "U" ]),
        ["Regal Caracal"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "W" ]),
        ["Resolute Reinforcements"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Revenge of the Rats"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "B" ]),
        ["Rise of the Dark Realms"] = new FoundationsCardMetadata(9, [ "Sorcery" ], [ "B" ]),
        ["Rite of Replication"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "U" ]),
        ["Rite of the Dragoncaller"] = new FoundationsCardMetadata(6, [ "Enchantment" ], [ "R" ]),
        ["River's Rebuke"] = new FoundationsCardMetadata(6, [ "Sorcery" ], [ "U" ]),
        ["Ruby, Daring Tracker"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "G", "R" ]),
        ["Rugged Highlands"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Run Away Together"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "U" ]),
        ["Rune-Scarred Demon"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "B" ]),
        ["Rune-Sealed Wall"] = new FoundationsCardMetadata(3, [ "Artifact Creature" ], [ "U" ]),
        ["Sanguine Indulgence"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "B" ]),
        ["Sanguine Syphoner"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Savage Ventmaw"] = new FoundationsCardMetadata(6, [ "Creature" ], [ "G", "R" ]),
        ["Scavenging Ooze"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Scorching Dragonfire"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "R" ]),
        ["Scoured Barrens"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Scrawling Crawler"] = new FoundationsCardMetadata(3, [ "Artifact Creature" ], []),
        ["Searslicer Goblin"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Seeker's Folly"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "B" ]),
        ["Seize the Spoils"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "R" ]),
        ["Selesnya Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Serra Angel"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "W" ]),
        ["Shipwreck Dowser"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "U" ]),
        ["Simic Guildgate"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Sire of Seven Deaths"] = new FoundationsCardMetadata(7, [ "Creature" ], []),
        ["Skyknight Squire"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Skyship Buccaneer"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "U" ]),
        ["Slagstorm"] = new FoundationsCardMetadata(3, [ "Sorcery" ], [ "R" ]),
        ["Slumbering Cerberus"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Snakeskin Veil"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "G" ]),
        ["Solemn Simulacrum"] = new FoundationsCardMetadata(4, [ "Artifact Creature" ], []),
        ["Sower of Chaos"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "R" ]),
        ["Spectral Sailor"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "U" ]),
        ["Sphinx of Forgotten Lore"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "U" ]),
        ["Spinner of Souls"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Springbloom Druid"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Stab"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "B" ]),
        ["Stasis Snare"] = new FoundationsCardMetadata(3, [ "Enchantment" ], [ "W" ]),
        ["Steel Hellkite"] = new FoundationsCardMetadata(6, [ "Artifact Creature" ], []),
        ["Storm Fleet Spy"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "U" ]),
        ["Strix Lookout"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "U" ]),
        ["Stroke of Midnight"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "W" ]),
        ["Stromkirk Noble"] = new FoundationsCardMetadata(1, [ "Creature" ], [ "R" ]),
        ["Strongbox Raider"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "R" ]),
        ["Sun-Blessed Healer"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Surrak, the Hunt Caller"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "G" ]),
        ["Swamp"] = new FoundationsCardMetadata(0, [ "Basic Land" ], []),
        ["Swiftblade Vindicator"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R", "W" ]),
        ["Swiftfoot Boots"] = new FoundationsCardMetadata(2, [ "Artifact" ], []),
        ["Swiftwater Cliffs"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Sylvan Scavenging"] = new FoundationsCardMetadata(3, [ "Enchantment" ], [ "G" ]),
        ["Tatyova, Benthic Druid"] = new FoundationsCardMetadata(5, [ "Legendary Creature" ], [ "G", "U" ]),
        ["Taurean Mauler"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "R" ]),
        ["Temple of Abandon"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Deceit"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Enlightenment"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Epiphany"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Malady"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Malice"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Mystery"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Plenty"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Silence"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Temple of Triumph"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Terror of Mount Velus"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "R" ]),
        ["Think Twice"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "U" ]),
        ["Thornweald Archer"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Thornwood Falls"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Thousand-Year Storm"] = new FoundationsCardMetadata(6, [ "Enchantment" ], [ "R", "U" ]),
        ["Thrashing Brontodon"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "G" ]),
        ["Three Tree Mascot"] = new FoundationsCardMetadata(2, [ "Artifact Creature" ], []),
        ["Thrill of Possibility"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "R" ]),
        ["Tinybones, Bauble Burglar"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "B" ]),
        ["Tolarian Terror"] = new FoundationsCardMetadata(7, [ "Creature" ], [ "U" ]),
        ["Tragic Banshee"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "B" ]),
        ["Tranquil Cove"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Treetop Snarespinner"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G" ]),
        ["Tribute to Hunger"] = new FoundationsCardMetadata(3, [ "Instant" ], [ "B" ]),
        ["Twinblade Paladin"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "W" ]),
        ["Twinflame Tyrant"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "R" ]),
        ["Uncharted Voyage"] = new FoundationsCardMetadata(4, [ "Instant" ], [ "U" ]),
        ["Unsummon"] = new FoundationsCardMetadata(1, [ "Instant" ], [ "U" ]),
        ["Valkyrie's Call"] = new FoundationsCardMetadata(5, [ "Enchantment" ], [ "W" ]),
        ["Valorous Stance"] = new FoundationsCardMetadata(2, [ "Instant" ], [ "W" ]),
        ["Vampire Gourmand"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Vampire Interloper"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Vampire Nighthawk"] = new FoundationsCardMetadata(3, [ "Creature" ], [ "B" ]),
        ["Vampiric Rites"] = new FoundationsCardMetadata(1, [ "Enchantment" ], [ "B" ]),
        ["Vanguard Seraph"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "W" ]),
        ["Vengeful Bloodwitch"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "B" ]),
        ["Venom Connoisseur"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Viashino Pyromancer"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "R" ]),
        ["Vile Entomber"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B" ]),
        ["Vivien Reid"] = new FoundationsCardMetadata(5, [ "Legendary Planeswalker" ], [ "G" ]),
        ["Vizier of the Menagerie"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G" ]),
        ["Volley Veteran"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "R" ]),
        ["Voracious Greatshark"] = new FoundationsCardMetadata(5, [ "Creature" ], [ "U" ]),
        ["Wardens of the Cycle"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "B", "G" ]),
        ["Wildborn Preserver"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "G" ]),
        ["Wilt-Leaf Liege"] = new FoundationsCardMetadata(4, [ "Creature" ], [ "G", "W" ]),
        ["Wind-Scarred Crag"] = new FoundationsCardMetadata(0, [ "Land" ], []),
        ["Witness Protection"] = new FoundationsCardMetadata(1, [ "Enchantment" ], [ "U" ]),
        ["Youthful Valkyrie"] = new FoundationsCardMetadata(2, [ "Creature" ], [ "W" ]),
        ["Zimone, Paradox Sculptor"] = new FoundationsCardMetadata(4, [ "Legendary Creature" ], [ "G", "U" ]),
        ["Zombify"] = new FoundationsCardMetadata(4, [ "Sorcery" ], [ "B" ]),
        ["Zul Ashur, Lich Lord"] = new FoundationsCardMetadata(2, [ "Legendary Creature" ], [ "B" ]),
    };

    public static bool TryGet(string cardName, out FoundationsCardMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            metadata = default!;
            return false;
        }

        return MetadataByName.TryGetValue(cardName, out metadata!);
    }
}
