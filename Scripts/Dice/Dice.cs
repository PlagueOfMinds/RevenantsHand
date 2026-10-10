using Godot;

public partial class Dice : RigidBody3D
{
    private Marker3D[] sides = new Marker3D[6];
	[Export] private Node diceSides;

    public override void _Ready()
    {
        for (int i = 0; i < 6; i++)
            sides[i] = diceSides.GetNode<Marker3D>($"side{i + 1}");
    }

    public int getRoll()
    {
        int result = 0;
        float highestZ = float.NegativeInfinity;

        for (int i = 0; i < sides.Length; i++)
        {
            float z = sides[i].GlobalPosition.Z; // world position, not Position
            if (z > highestZ)
            {
                highestZ = z;
                result = i + 1;
            }
        }
        return result;
    }
}