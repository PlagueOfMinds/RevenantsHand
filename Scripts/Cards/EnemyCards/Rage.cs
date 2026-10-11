using Godot;
using System.Threading.Tasks;

// Rage: Only if took 10 or more last turn; deal [2X].
public partial class Rage : Card
{
	public override CardEffect Effect => CardEffect.Damage;
	public override int GetAmount(int dieValue) => dieValue * 2;

	// A battle condition, not a die condition, so it goes in CanUse
	public override bool CanUse(Combatant user, Combatant opponent) =>
		user.damageTakenSinceLastTurn >= 10;

	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if (CanUse(user, opponent))
			opponent.TakeDamage(GetAmount(value));

		return Task.CompletedTask;
	}
}
