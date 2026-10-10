using Godot;
using System;
using System.Threading.Tasks;

public partial class Shifty : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        if(token.Value%2  == 0)
        {
            // On even, subtract 1
            token.Die.bump(-1);
        } else
        {
            token.Die.bump(1);
        }
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
