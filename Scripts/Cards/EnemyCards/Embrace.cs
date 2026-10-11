using Godot;
using System.Threading.Tasks;

// Embrace: Use only with [3]: Deal 3 Poison.
public partial class Embrace : Card
{
	public override CardEffect Effect => CardEffect.Poison;
	public override bool AcceptsValue(int dieValue) => dieValue == 3;
	public override int GetAmount(int dieValue) => 2;

	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if (AcceptsValue(value))
			opponent.AddPoison(GetAmount(value));

		return Task.CompletedTask;
	}
}
