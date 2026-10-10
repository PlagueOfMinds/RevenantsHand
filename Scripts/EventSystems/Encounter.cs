using Godot;
using System;

// Remembers which enemy the next battle is against, across scene changes
public static class Encounter
{
    public const string CombatScenePath = "res://Scenes/Combat.tscn";

    // Used when the Combat scene is run directly (F6) without choosing an enemy first
    private static Func<Enemy> nextEnemy = () => new Revenant();

    // Call from a map, menu, or event to start a fight
    public static void Begin(Node caller, Func<Enemy> createEnemy)
    {
        nextEnemy = createEnemy;
        caller.GetTree().ChangeSceneToFile(CombatScenePath);
    }

    public static Enemy CreateEnemy() => nextEnemy();
}