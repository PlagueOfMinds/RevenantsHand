using Godot;
using System.Threading.Tasks;

public partial class BAHH : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        gameManager.Player.TakeDamage(value-2);
        return Task.CompletedTask;
    }
}
