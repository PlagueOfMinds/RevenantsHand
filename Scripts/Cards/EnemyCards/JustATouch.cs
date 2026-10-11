using Godot;
using System.Threading.Tasks;

public partial class JustATouch : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value >= 4)
        {
            gameManager.Player.TakeDamage(value*2);
        }
        
        return Task.CompletedTask;
    }
}
