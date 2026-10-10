using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Everything about the current run that has to survive scene changes
public static class Run
{
    // ---- Scenes (use right-click > Copy Path in the FileSystem dock) ----
    public const string CombatScenePath = "res://Scenes/Combat.tscn";
    public const string RewardScenePath = "res://Scenes/RewardScreen.tscn";

    // ---- Run setup ----
    public const int StartingMaxHealth = 30;
    public const int OfferCount = 3;

    // The player's starting cards, left to right
    private static readonly string[] StartingDeck =
    {
        "res://Scenes/Cards/AttackCard.tscn",
        "res://Scenes/Cards/DefendCard.tscn",
        "res://Scenes/Cards/HealCard.tscn",
    };

    // Cards the Revenant can offer between fights
    private static readonly string[] CardPool =
    {
        "res://Scenes/Cards/AttackCard.tscn",
        "res://Scenes/Cards/DefendCard.tscn",
        "res://Scenes/Cards/HealCard.tscn",
        // add more card scenes here
    };

    // Fights in order; the run is won after the last one
    private static readonly Func<Enemy>[] Encounters =
    {
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

    public static void StartNewRun()
    {
        IsActive = true;
        PlayerMaxHealth = StartingMaxHealth;
        PlayerHealth = StartingMaxHealth;
        Deck = new List<string>(StartingDeck);
        EncounterIndex = 0;
    }

    public static void BeginCurrentEncounter() => Encounter.Begin(Encounters[EncounterIndex]);

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

    // Random cards from the pool, preferring ones not already in the deck
    public static List<string> RollOffers()
    {
        var options = CardPool.Where(path => !Deck.Contains(path)).ToList();
        if (options.Count < OfferCount) options = CardPool.ToList();
        return options.OrderBy(_ => rng.Next()).Take(OfferCount).ToList();
    }

    // The new card takes the old card's slot, so left-to-right order is kept
    public static void SwapCard(int deckIndex, string newCardPath) => Deck[deckIndex] = newCardPath;

    private static void EndRun(bool won)
    {
        GD.Print(won ? "Run won!" : "Run lost.");
        // No menu yet: start a fresh run. Later, send this to your title/menu scene instead.
        StartNewRun();
        BeginCurrentEncounter();
    }
}