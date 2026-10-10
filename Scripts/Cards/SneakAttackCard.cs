using Godot;
using System.Threading.Tasks;

public partial class SneakAttackCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value < 4)
        {
            gameManager.Enemy.TakeDamage(5);
        }
        return Task.CompletedTask;
    }
}
