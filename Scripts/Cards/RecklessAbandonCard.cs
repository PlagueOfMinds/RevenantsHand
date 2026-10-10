using Godot;
using System;
using System.Threading.Tasks;

public partial class RecklessAbandonCard : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        token.Die.bump(6); // guarantee a 6
        gameManager.Player.TakeDamage(2);
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
