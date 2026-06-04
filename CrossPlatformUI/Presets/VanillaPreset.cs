using Z2Randomizer.RandomizerCore;

namespace CrossPlatformUI.Presets;

/// <summary>
/// Vanilla and Default preset.
/// </summary>
public static class VanillaPreset
{
    public static readonly RandomizerConfiguration Preset = new()
    {
        //Start
        StartingLives = StartingLives.Lives3,
        IndeterminateOptionRate = IndeterminateOptionRate.HALF,

        //Overworld
        PalacesCanSwapContinents = false,
        ShuffleGP = false,
        ShuffleEncounters = false,
        EncounterRate = EncounterRate.NORMAL,
        RiverDevilBlockerOption = RiverDevilBlockerOption.PATH,
        EastRocks = true,
        GenerateBaguWoods = true,
        RestrictConnectionCaveShuffle = true,
        GoodBoots = false,
        HidePalace = true,
        HideKasuto = true,
        ShuffleWhichLocationIsHidden = false,

        //Palaces
        PalaceItemRoomCount = PalaceItemRoomCount.ONE,
        NormalPalaceLength = PalaceLengthOption.FULL,
        GpLength = PalaceLengthOption.FULL,
        IncludeExpertRooms = true,
        TBirdRequired = true,
        PalacesToCompleteMin = 6,
        PalacesToCompleteMax = 6,

        //Spells
        ShuffleSpellLocations = false,
        DisableMagicContainerRequirements = false,
        RandomizeSpellSpellEnemy = false,
        SwapUpAndDownStab = false,
        FireOption = FireOption.NORMAL,

        //Enemies
        ShuffleOverworldEnemies = false,
        ShufflePalaceEnemies = false,
        DripperEnemyOption = DripperEnemyOption.ONLY_BOTS,
        MixLargeAndSmallEnemies = false,

        //Items
        ShufflePalaceItems = false,
        ShuffleOverworldItems = false,
        MixOverworldAndPalaceItems = false,
        IncludePBagCavesInItemShuffle = false,
        IncludeSwordTechsInShuffle = false,
        IncludeQuestItemsInShuffle = false,
        IncludeSpellsInShuffle = false,
        IncludeBagusNoteInShuffle = false,

        ShuffleSmallItems = false,
        RemoveSpellItems = false,
        ShufflePBagAmounts = false,
        PalacesContainExtraKeys = false,
        RandomizeNewKasutoJarRequirements = false,

        //Hints
        EnableHelpfulHints = false,
        EnableSpellItemHints = false,
        EnableTownNameHints = false,
    };
}
