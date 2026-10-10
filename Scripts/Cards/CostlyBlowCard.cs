using Godot;
using System.Threading.Tasks;

public partial class CostlyBlowCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        gameManager.Enemy.TakeDamage(2*value);
        gameManager.Player.TakeDamage(value);
        return Task.CompletedTask;
    }
}
