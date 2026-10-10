using Godot;

public partial class Dice : RigidBody3D
{
    [Export] private Node3D diceSides;

    private Node3D[] sides = new Node3D[6];

    public override void _Ready()
    {
        for (int i = 0; i < 6; i++)
        {
            sides[i] = diceSides.GetNode<Node3D>($"side{i + 1}");

            if (!IsAncestorOf(sides[i]))
                GD.PushError($"Dice: side{i + 1} is not inside this die, so it won't move with it.");
        }
    }

    public int getRoll()
    {
        int result = 0;
        float highestY = float.NegativeInfinity;

        for (int i = 0; i < sides.Length; i++)
        {
            float y = sides[i].GlobalPosition.Y;
            if (y > highestY)
            {
                highestY = y;
                result = i + 1;
            }
        }
        return result;
    }
}