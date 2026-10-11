using Godot;
using System.Threading.Tasks;

public partial class DontBite : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Poison;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if(value%2 == 1)
		{
			gameManager.Player.AddPoison(value);
		}
		
		return Task.CompletedTask;
	}
}
