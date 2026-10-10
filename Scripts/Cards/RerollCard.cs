using Godot;
using System;
using System.Threading.Tasks;

public partial class RerollCard : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        GD.Print($"{CardName} rerolled and got: {token.Value}");
        disableSelf();
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
