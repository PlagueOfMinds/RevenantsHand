using Godot;
using System.Threading.Tasks;

// The Hand That Feeds: Use only with [Even]: Heal [2X].
public partial class TheHandThatFeeds : Card
{
	public override CardEffect Effect => CardEffect.Heal;
	public override bool AcceptsValue(int dieValue) => dieValue % 2 == 0;
	public override int GetAmount(int dieValue) => dieValue;

	public override Task Resolve(Combatant user, Combatant opponent, int value)
	{
		if (AcceptsValue(value))
			user.Heal(GetAmount(value));

		return Task.CompletedTask;
	}
}
