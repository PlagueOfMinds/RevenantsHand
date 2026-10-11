using System;
using System.Collections.Generic;
using System.Linq;

// How an enemy values each kind of outcome; each enemy class picks its own
public record AiProfile(
    float DamageWeight = 1f,     // value of 1 damage dealt now
    float HealWeight = 0.6f,     // value of 1 HP restored at full safety...
    float HealUrgency = 1.5f,    // ...plus this much more as its health drops
    float PoisonWeight = 0.6f,   // value of 1 future poison damage (delayed, so worth less)
    float LethalBonus = 1000f)   // finishing the player beats everything
{
    public static AiProfile Default { get; } = new AiProfile();
}

// Shared decision-making for every enemy: tries every way to put its dice on its cards
public static class EnemyAI
{
    public static List<QueuedAction> PlanTurn(Enemy self, Combatant opponent, IReadOnlyList<Card> cards,
                                              int maxMoves, AiProfile profile)
    {
        int[] dice = self.Dice.OrderByDescending(d => d).ToArray(); // try high dice first
        List<Card> usable = cards.Where(c => c.CanUse(self, opponent)).ToList();
        bool[] used = new bool[usable.Count];

        var current = new List<(Card Card, int Value)>();
        var best = new List<(Card Card, int Value)>();
        float bestScore = 0f;

        void Search(int dieIndex)
        {
            if (dieIndex == dice.Length || current.Count == maxMoves)
            {
                float score = ScorePlan(current, self, opponent, profile);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new List<(Card, int)>(current);
                }
                return;
            }

            int die = dice[dieIndex];

            // Put this die on any free card that takes it and actually does something with it
            for (int c = 0; c < usable.Count; c++)
            {
                if (used[c] || !usable[c].AcceptsValue(die) || !usable[c].GetEffects(die).Any()) continue;

                used[c] = true;
                current.Add((usable[c], die));
                Search(dieIndex + 1);
                current.RemoveAt(current.Count - 1);
                used[c] = false;
            }

            // Or leave this die unused
            Search(dieIndex + 1);
        }

        Search(0);

        // Resolve left to right in EnemyCardRow, like the player's cards
        var order = cards.ToList();
        return best
            .OrderBy(move => order.IndexOf(move.Card))
            .Select(move => new QueuedAction(self, opponent, move.Card, move.Value))
            .ToList();
    }

    private static float ScorePlan(List<(Card Card, int Value)> plan, Enemy self, Combatant opponent, AiProfile p)
    {
        if (plan.Count == 0) return 0f;

        int damage = 0, heal = 0, poison = 0;
        foreach (var (card, value) in plan)
        {
            foreach (CardOutcome outcome in card.GetEffects(value))
            {
                switch (outcome.Effect)
                {
                    case CardEffect.Damage: damage += outcome.Amount; break;
                    case CardEffect.Heal:   heal   += outcome.Amount; break;
                    case CardEffect.Poison: poison += outcome.Amount; break;
                }
            }
        }

        float score = 0f;

        // Damage: overkill is worthless, a kill beats everything
        if (damage >= opponent.Health) score += p.LethalBonus;
        score += p.DamageWeight * Math.Min(damage, opponent.Health);

        // Healing: only HP actually restored counts, and it's worth more the closer the enemy is to dying.
        // The enemy's own poison ticks at the end of this turn, so count it as already lost.
        int restored = Math.Min(heal, self.MaxHealth - self.Health);
        int healthAfterPoison = self.Health - self.Poison;
        float danger = Math.Clamp(1f - (float)healthAfterPoison / self.MaxHealth, 0f, 1f);
        score += p.HealWeight * (1f + p.HealUrgency * danger) * restored;

        // Healing that saves it from dying to its own poison
        if (healthAfterPoison <= 0 && healthAfterPoison + restored > 0) score += p.LethalBonus * 0.5f;

        // Poison: the extra total damage it adds over the coming turns (a stack of N deals N + (N-1) + ... + 1)
        if (poison > 0)
        {
            int extra = Triangle(opponent.Poison + poison) - Triangle(opponent.Poison);
            score += p.PoisonWeight * Math.Min(extra, opponent.Health);
        }

        return score;
    }

    private static int Triangle(int n) => n * (n + 1) / 2;
}