using Godot;
using System.Threading.Tasks;

public partial class ScrappyCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(value < 3)
        {
            gameManager.Enemy.TakeDamage(4);
            gameManager.Player.Heal(4);
        }
        return Task.CompletedTask;
    }
}
