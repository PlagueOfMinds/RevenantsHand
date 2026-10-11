using Godot;
using System.Threading.Tasks;

public partial class Embrace : Card
{
	// What the AI reads: the card's type...
	public override CardEffect Effect => CardEffect.Poison;
	
	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		gameManager.Player.poisonCount += 3;
		return Task.CompletedTask;
	}
}
