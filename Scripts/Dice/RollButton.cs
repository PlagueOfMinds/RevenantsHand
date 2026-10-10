using Godot;

public partial class RollButton : Button
{
    [Export] private DiceController diceController;
    [Export] private DiceTokenLayer tokenLayer;

    public override void _Ready()
    {
        Pressed += OnRollPressed;
    }

    private async void OnRollPressed()
    {
        Disabled = true;
        tokenLayer.ClearTokens();

        Dice[] dice = await diceController.rollDice();
        tokenLayer.CreateTokens(dice, diceController);

        GD.Print("Rolled: " + string.Join(", ", diceController.LastResults));
        Disabled = false;
    }
}