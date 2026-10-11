using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Everything about the current run that has to survive scene changes
public static class Run
{
    public const string CombatScenePath = "res://Scenes/BigScenes/Main.tscn";
    public const string RewardScenePath = "res://Scenes/BigScenes/RewardsEncounter.tscn";
    public const string StartMenuScenePath = "res://Scenes/BigScenes/StartMenu.tscn";
    public const string CardFolder = "res://Scenes/Cards";

    // ---- Run setup ----
    public const int StartingMaxHealth = 30;
    public const int OfferCount = 3;

    // The player's starting cards, left to right

    public static Enemy CreateCurrentEnemy() => Encounters[EncounterIndex]();

    private static readonly string[] StartingDeck =
    {
        "res://Scenes/Cards/AttackCard.tscn",
        "res://Scenes/Cards/DefendCard.tscn",
        "res://Scenes/Cards/HealCard.tscn",
    };

    // Cards the Revenant can offer between fights
    private static readonly string[] CardPool =
    {
        $"{CardFolder}/ArmegeddonCard.tscn",
        $"{CardFolder}/AttackCard.tscn",
        $"{CardFolder}/CostlyBlowCard.tscn",
        $"{CardFolder}/DefendCard.tscn",
        $"{CardFolder}/HealCard.tscn",
        $"{CardFolder}/LifeStakesCard.tscn",
        $"{CardFolder}/PanaceaCard.tscn",
        $"{CardFolder}/RecklessAbandonCard.tscn",
        $"{CardFolder}/RevengeCard.tscn",
        $"{CardFolder}/ScrappyCard.tscn",
        $"{CardFolder}/ShiftyCard.tscn",
        $"{CardFolder}/SneakAttackCard.tscn",
        $"{CardFolder}/StruggleCard.tscn",
        $"{CardFolder}/ThrashCard.tscn",
        $"{CardFolder}/Tough.tscn",
    };

    // Fights in order; the run is won after the last one
    private static readonly Func<Enemy>[] Encounters =
    {
        () => new Goat(),
        () => new Hands(),
        () => new Revenant(),
        // () => new SomeOtherEnemy(),
    };

    // ---- Run state ----
    public static bool IsActive { get; private set; }
    public static int PlayerMaxHealth { get; private set; }
    public static int PlayerHealth { get; private set; }
    public static List<string> Deck { get; private set; } = new();
    public static int EncounterIndex { get; private set; }
    public static int EncounterCount => Encounters.Length;

    private static readonly Random rng = new();

    public const string BossTransitionScenePath = "res://Scenes/Transitions/RevenantTransition.tscn";

    public static bool IsFinalEncounter => EncounterIndex == Encounters.Length - 1;

    public static void BeginCurrentEncounter() =>
        Encounter.Begin(Encounters[EncounterIndex], IsFinalEncounter ? BossTransitionScenePath : null);

    public static void StartNewRun()
    {
        IsActive = true;
        PlayerMaxHealth = StartingMaxHealth;
        PlayerHealth = StartingMaxHealth;
        Deck = new List<string>(StartingDeck);
        EncounterIndex = 0;
    }

    // Called by GameManager when the enemy dies
    public static void BattleWon(int playerHealthLeft)
    {
        PlayerHealth = playerHealthLeft;
        EncounterIndex++;

        if (EncounterIndex >= Encounters.Length) EndRun(true);
        else SceneTransition.Instance.GoTo(RewardScenePath);
    }

    // Called by GameManager when the player dies
    public static void BattleLost() => EndRun(false);

    // Called by the reward screen when the player is done
    public static void ContinueFromReward() => BeginCurrentEncounter();

    
    // Random cards the player doesn't already have, with no repeats.
    // Returns fewer than OfferCount if the pool runs out; the reward screen leaves those slots empty.
    public static List<string> RollOffers()
    {
        return CardPool
            .Distinct()                              // ignore accidental duplicates in the pool list
            .Where(path => !Deck.Contains(path))     // never offer a card the player already has
            .OrderBy(_ => rng.Next())                // shuffle
            .Take(OfferCount)
            .ToList();
    }

    // The new card takes the old card's slot, so left-to-right order is kept
    public static void SwapCard(int deckIndex, string newCardPath) => Deck[deckIndex] = newCardPath;

    // Start a fresh run from the menu: the Revenant's offer first, then fight 1
    public static void BeginRun()
    {
        StartNewRun();                                   // fresh health, starting deck, encounter 0
        SceneTransition.Instance.GoTo(RewardScenePath); // Revenant's offer before the first fight
    }
    private static void EndRun(bool won)
    {
        GD.Print(won ? "Run won!" : "Run lost.");
        IsActive = false;
        SceneTransition.Instance.GoTo(StartMenuScenePath);
    }
}