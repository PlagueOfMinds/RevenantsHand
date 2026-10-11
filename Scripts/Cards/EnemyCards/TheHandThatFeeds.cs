using Godot;
using System.Threading.Tasks;

public partial class TheHandThatFeeds : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value%2 == 0)
        {
            gameManager.Enemy.Heal(2 * value);
        }
        
        return Task.CompletedTask;
    }
}
