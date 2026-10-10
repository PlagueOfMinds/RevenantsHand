using Godot;
using System.Threading.Tasks;

public partial class PanaceaCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(gameManager.Player.Health%2 == 1)
        {
            // on odd health, heal double
            gameManager.Player.Heal(value * 2);
        }
        return Task.CompletedTask;
    }
}
