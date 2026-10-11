using Godot;
using System.Threading.Tasks;

// Just a Touch: Use only with [4] or greater: Deal [2X] damage.
public partial class JustATouch : Card
{
	public override CardEffect Effect => CardEffect.Damage;
	public override bool AcceptsValue(int dieValue) => dieValue >= 4;
	public override int GetAmount(int dieValue) => dieValue;

	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if (AcceptsValue(value))
			opponent.TakeDamage(GetAmount(value));

		return Task.CompletedTask;
	}
}
