using Godot;
using System.Threading.Tasks;

public partial class TheHandThatFeeds : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Heal;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if(value%2 == 0)
		{
			gameManager.Enemy.Heal(2 * value);
		}
		
		return Task.CompletedTask;
	}
}
