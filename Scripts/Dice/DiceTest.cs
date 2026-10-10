using Godot;

public partial class DiceTest : Button
{
    [Export] private DiceController diceController;

    public override void _Ready()
    {
        Pressed += OnRollPressed;
        diceController.DiceRolled += OnDiceRolled;
    }

    private void OnRollPressed()
    {
        Disabled = true; // prevent re-rolling while dice are still moving
        diceController.rollDice();
    }

    private void OnDiceRolled(int[] results)
    {
        Disabled = false;
        GD.Print("Rolled: " + string.Join(", ", results));
    }
}