using Godot;
using System;
using System.Threading.Tasks;

public partial class ThrashCard : Card
{
    public override void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        gameManager.Enemy.TakeDamage(token.Value);
        SlashFx.Instance.Play();


        var controller = gameManager.getPlayerDice();
        var layer = gameManager.getDiceTokenLayer();

        // Grab what we need BEFORE anything gets freed
        Dice oldDie = token.Die; // use whatever your DiceToken calls its die reference

        // Defer so we don't modify the list or free nodes mid-drag-handler
        Callable.From(() =>
        {
            layer.RemoveToken(token);
            controller.RemoveDie(oldDie);
        }).CallDeferred();
    }

    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"This should never happen! A reroll can't resolve!");
        return Task.CompletedTask;
    }
}
