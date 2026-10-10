using Godot;
using System.Collections.Generic;

public partial class DiceTokenLayer : Control
{
    [Export] private float tokenWorldSize = 1.5f; // world units around each die; keep larger than the die

    private readonly List<DiceToken> tokens = new();
    public IReadOnlyList<DiceToken> Tokens => tokens;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; // clicks pass through except on tokens
    }

    public void CreateTokens(Dice[] dice, DiceController controller)
    {
        ClearTokens();

        float ppu = controller.GetScreenPixelsPerUnit();
        Basis camBasis = controller.TrayCamera.GlobalBasis.Orthonormalized();

        foreach (var die in dice)
        {
            Vector2 center = controller.GetCanvasPosition(die); // read before the die leaves the tray

            var token = new DiceToken();
            AddChild(token);
            token.Setup(die, camBasis, tokenWorldSize, ppu);
            token.GlobalPosition = center - token.Size / 2f;
            tokens.Add(token);
        }
    }

    public void ClearTokens()
    {
        foreach (var token in tokens)
            if (IsInstanceValid(token)) token.QueueFree();
        tokens.Clear();
    }
}