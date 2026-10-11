using Godot;
using System.Threading.Tasks;

public partial class OnThisQuietNight : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value == 6)
        {
            gameManager.Player.TakeDamage(12);
        }
        
        return Task.CompletedTask;
    }
}
