using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

// Mesmerize: Use only with [2] or less: Heal [X]. Deal 1 Poison.
public partial class Mesmerize : Card
{
    public override bool AcceptsValue(int dieValue) => dieValue <= 2;

    // Two effects, so the AI reads both from here instead of from Effect
    public override IEnumerable<CardOutcome> GetEffects(int dieValue)
    {
        yield return new CardOutcome(CardEffect.Heal, dieValue);
        yield return new CardOutcome(CardEffect.Poison, 1);
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if (AcceptsValue(value))
        {
            user.Heal(value);
            opponent.AddPoison(1);
        }

        return Task.CompletedTask;
    }
}