using Godot;
using System.Threading.Tasks;

public partial class Mesmerize : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Poison;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if(value <= 2)
		{
			gameManager.Enemy.Heal(value);
			gameManager.Player.poisonCount += 1;
		}
		
		return Task.CompletedTask;
	}
}
