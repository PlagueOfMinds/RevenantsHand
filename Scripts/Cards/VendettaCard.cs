using Godot;
using System.Threading.Tasks;

public partial class VendettaCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if (value % 2 == 0)
        {
            // On Even, heal
            gameManager.Player.Heal(value);
        }
        else
        {
            // On odd, attack
            gameManager.Enemy.TakeDamage(value);
            SlashFx.Instance.Play();
        }
        return Task.CompletedTask;
    }
}
