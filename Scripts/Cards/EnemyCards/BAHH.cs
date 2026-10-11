using Godot;
using System;
using System.Threading.Tasks;

public partial class BAHH : Card
{
    // What the AI reads: the card's type...
    public override CardEffect Effect => CardEffect.Damage;

    // amount it is for a die X (BAHH!: Deal [X-2])
    public override int GetAmount(int dieValue) => Math.Max(0, dieValue - 2);

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        opponent.TakeDamage(GetAmount(value));
        return Task.CompletedTask;
    }
}