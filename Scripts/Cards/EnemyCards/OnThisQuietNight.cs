using Godot;
using System.Threading.Tasks;

// On This Quiet Night: Use only with [6]: Deal 12 damage.
public partial class OnThisQuietNight : Card
{
	public override CardEffect Effect => CardEffect.Damage;
	public override bool AcceptsValue(int dieValue) => dieValue == 6;
	public override int GetAmount(int dieValue) => 12;

	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if (AcceptsValue(value))
			opponent.TakeDamage(GetAmount(value));

		return Task.CompletedTask;
	}
}
