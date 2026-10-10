using Godot;
using System.Threading.Tasks;

public partial class LifeStakesCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        gameManager.Player.Heal(2 * value + 2);
        gameManager.Player.poisonCount += 3;
        return Task.CompletedTask;
    }
}
