using Godot;
using System;
using System.Collections.Generic;

public partial class Combatant : Node2D
{
    private int maxHealth = 30;

    public string DisplayName { get; set; } = "Combatant";

    // Setting MaxHealth also refills Health
    public int MaxHealth
    {
        get => maxHealth;
        set { maxHealth = value; Health = value; }
    }

    public int Health { get; private set; } = 30;
    public int Block { get; private set; }
    public bool IsDead => Health <= 0;

    // This turn's dice, plus a record of every turn's dice
    public int[] Dice { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<int[]> DiceHistory => diceHistory;
    private readonly List<int[]> diceHistory = new();

    public event Action<Combatant> Changed;
    public event Action<Combatant> Died;

    public void SetDice(int[] dice)
    {
        Dice = (int[])dice.Clone();
        diceHistory.Add(Dice);
        Changed?.Invoke(this);
    }

    // Block absorbs damage first; returns the damage that got through
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return 0;

        int blocked = Math.Min(Block, amount);
        Block -= blocked;
        int dealt = amount - blocked;
        Health = Math.Max(0, Health - dealt);

        Changed?.Invoke(this);
        if (IsDead) Died?.Invoke(this);
        return dealt;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        Health = Math.Min(MaxHealth, Health + amount);
        Changed?.Invoke(this);
    }

    public void AddBlock(int amount)
    {
        if (amount <= 0) return;
        Block += amount;
        Changed?.Invoke(this);
    }

    // New turn: block wears off, dice clear until the next roll
    public virtual void StartTurn()
    {
        Block = 0;
        Dice = Array.Empty<int>();
        Changed?.Invoke(this);
    }
}

// One card effect waiting to resolve: who used it, against whom, which card, and the die value
public record QueuedAction(Combatant User, Combatant Opponent, Card Card, int Value);