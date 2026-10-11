using Godot;
using System.Threading.Tasks;

public partial class Rage : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(gameManager.Enemy.damageTakenSinceLastTurn >= 10)
        {
            gameManager.Player.TakeDamage(value*2);
        }
        
        return Task.CompletedTask;
    }
}
