using Godot;
using System.Threading.Tasks;

public partial class Rage : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Damage;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if(gameManager.Enemy.damageTakenSinceLastTurn >= 10)
		{
			gameManager.Player.TakeDamage(value*2);
		}
		
		return Task.CompletedTask;
	}
}
