using Godot;
using System;
using System.Threading.Tasks;

public partial class StruggleCard : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        if(token.Value != 1)
        {
            gameManager.Player.TakeDamage(1);
            gameManager.Enemy.TakeDamage(1);
        }
        int newValue = token.Die.bump(-1);
        
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
