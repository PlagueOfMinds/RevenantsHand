using Godot;
using System.Threading.Tasks;

public partial class AttackCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        gameManager.Enemy.TakeDamage(value);
        return Task.CompletedTask;
    }
}
