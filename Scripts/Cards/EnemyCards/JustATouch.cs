using Godot;
using System.Threading.Tasks;

public partial class JustATouch : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Damage;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if(value >= 4)
		{
			gameManager.Player.TakeDamage(value*2);
		}
		
		return Task.CompletedTask;
	}
}
