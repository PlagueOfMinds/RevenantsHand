using System;
using System.Collections.Generic;
using System.Linq;

public abstract class Combatant
{
    public string Name { get; }
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int Block { get; private set; }
    public bool IsDead => Health <= 0;

    // This turn's dice, plus a record of every turn's dice
    public int[] Dice { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<int[]> DiceHistory => diceHistory;
    private readonly List<int[]> diceHistory = new();

    public event Action<Combatant> Changed;
    public event Action<Combatant> Died;

    protected Combatant(string name, int maxHealth)
    {
        Name = name;
        MaxHealth = maxHealth;
        Health = maxHealth;
    }

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

public class Player : Combatant
{
    public Player(int maxHealth) : base("Player", maxHealth) { }
}

public class Enemy : Combatant
{
    private readonly Random rng = new();

    public Enemy(string name, int maxHealth) : base(name, maxHealth) { }

    // One move per turn. Default: a random enemy card, powered by the total of all its dice
    public virtual QueuedAction ChooseMove(IReadOnlyList<Card> cards, Combatant opponent)
    {
        if (cards.Count == 0 || Dice.Length == 0) return null;

        Card card = cards[rng.Next(cards.Count)];
        return new QueuedAction(this, opponent, card, Dice.Sum());
    }
}

// One card effect waiting to resolve: who used it, against whom, which card, and the die value
public record QueuedAction(Combatant User, Combatant Opponent, Card Card, int Value);