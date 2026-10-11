using Godot;
using System.Threading.Tasks;

public partial class RevengeCard : Card
{
    public override Task Resolve(Combatant user, Combatant opponent, int value)
    {
        if(gameManager.Player.Health <= 10)
        {
            gameManager.Enemy.TakeDamage(value);
            gameManager.Player.Heal(value);
            SlashFx.Instance.Play();
        }
        return Task.CompletedTask;
    }
}
