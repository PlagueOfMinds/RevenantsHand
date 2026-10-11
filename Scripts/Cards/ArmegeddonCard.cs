using Godot;
using System.Threading.Tasks;

public partial class ArmegeddonCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(gameManager.Player.Health%2 == 1)
        {
            // on odd health, deal double
            SlashFx.Instance.Play();
            gameManager.Enemy.TakeDamage(value * 2);
        }
        return Task.CompletedTask;
    }
}
