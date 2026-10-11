using Godot;
using System.Threading.Tasks;

public partial class Embrace : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        gameManager.Player.poisonCount += 3;
        return Task.CompletedTask;
    }
}
