using Godot;
using System.Threading.Tasks;

public partial class Mesmerize : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value <= 2)
        {
            gameManager.Enemy.Heal(value);
            gameManager.Player.poisonCount += 1;
        }
        
        return Task.CompletedTask;
    }
}
