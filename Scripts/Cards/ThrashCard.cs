using Godot;
using System;
using System.Threading.Tasks;

public partial class ThrashCard : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        gameManager.Enemy.TakeDamage(token.Value);
        // Get rid of the dice
        token.QueueFree();
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
