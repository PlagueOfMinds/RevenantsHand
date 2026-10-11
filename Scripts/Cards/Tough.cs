using Godot;
using System;
using System.Threading.Tasks;

public partial class Tough : Card
{
    
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        int newValue = token.Die.bump(2);
        gameManager.Player.Heal(2);

        
        disableSelf();
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }

}
