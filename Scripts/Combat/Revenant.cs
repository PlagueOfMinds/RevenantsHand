using Godot;
using System;

public partial class Revenant : Enemy
{
	public Revenant() : base("Revenant", 60) {}

	protected override string TexturePath => "";

	// This boss hits twice as hard
	// Implement boss ai
	public override QueuedAction ChooseMove(IReadOnlyList<Card> cards, Combatant opponent)
    {
        QueuedAction move = base.ChooseMove(cards, opponent);
        return move == null ? null : move with { Value = move.Value * 2 };
    }
}
