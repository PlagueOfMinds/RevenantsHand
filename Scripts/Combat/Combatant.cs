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
    public int Poison { get; private set; }
    public bool IsDead => Health <= 0;
    public int poisonCount = 0;
    public int damageTakenSinceLastTurn;
    public int damageTakenSinceLastTurnT;

    public bool IsPlayer { get; set; } = false;


    // This turn's dice, plus a record of every turn's dice
    public int[] Dice { get; private set; } = Array.Empty<int>();
    public IReadOnlyList<int[]> DiceHistory => diceHistory;
    private readonly List<int[]> diceHistory = new();

    public event Action<Combatant> Changed;
    public event Action<Combatant> Died;

    public void SetHealth(int value)
    {
        Health = Math.Clamp(value, 0, MaxHealth);
        Changed?.Invoke(this);
    }

    public void SetDice(int[] dice)
    {
        Dice = (int[])dice.Clone();
        diceHistory.Add(Dice);
        Changed?.Invoke(this);
    }

    // Lose HP from any source (cards, poison). Returns the HP actually lost.
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return 0;
        int dealt = amount;
        Health = Math.Max(0, Health - dealt);
        damageTakenSinceLastTurnT += amount;
        int lost = Math.Min(amount, Health);

        ScreenShake.Shake(this, strength: Mathf.Min(amount * 1.5f, 30f));
        if (IsPlayer && amount >= ScreenFlash.DamageThreshold)              // <-- new
            ScreenFlash.Flash(this, ScreenFlash.DamageColor);

        Changed?.Invoke(this);
        if (IsDead) Died?.Invoke(this);
        return lost;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        Health = Math.Min(MaxHealth, Health + amount);
        Changed?.Invoke(this);

        if (IsPlayer) ScreenFlash.Flash(this, ScreenFlash.HealColor);
    }

    public void AddPoison(int amount)
    {
        if (amount <= 0 || IsDead) return;
        Poison += amount;
        Changed?.Invoke(this);
    }

    // End of this combatant's turn: lose HP equal to poison, then poison drops by 1.
    // Returns the damage dealt.
    public int TickPoison()
    {
        if (Poison <= 0 || IsDead) return 0;

        int damage = Poison;
        Poison--;
        return TakeDamage(damage);
    }

    // Called by GameManager when this combatant's turn is fully over
    public void EndOwnTurn() => damageTakenSinceLastTurn = 0;

    // New turn: dice clear until the next roll
    public virtual void StartTurn()
    {
        Dice = Array.Empty<int>();
        Changed?.Invoke(this);
        TakeDamage(poisonCount);
        if(poisonCount > 0)
        {
            poisonCount--;
        }
        damageTakenSinceLastTurn = damageTakenSinceLastTurnT;
        damageTakenSinceLastTurnT = 0;
        
    }
}

// One card effect waiting to resolve: who used it, against whom, which card, and the die value
public record QueuedAction(Combatant User, Combatant Opponent, Card Card, int Value);