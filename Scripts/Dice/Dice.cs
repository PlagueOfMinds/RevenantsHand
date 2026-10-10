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

    // Transform that turns the given side straight up and rests the die flat on floorY
    public Transform3D GetUprightTransform(int side, float? floorY)
    {
        Vector3 center = GlobalPosition;
        Vector3 toSide = sides[side - 1].GlobalPosition - center;

        // Smallest rotation that points this side straight up
        Quaternion tilt = new Quaternion(toSide.Normalized(), Vector3.Up);
        Quaternion current = GlobalBasis.GetRotationQuaternion();
        Basis basis = new Basis(tilt * current).Scaled(GlobalBasis.Scale);

        // Flat on the floor: the center sits one marker-distance above it
        float y = floorY.HasValue ? floorY.Value + toSide.Length() : center.Y;
        return new Transform3D(basis, new Vector3(center.X, y, center.Z));
    }

    public int reroll()
    {
        int currentSide = this.getRoll();
        int roll = GD.RandRange(1, 6);

        // Change our upright side
        var exclude = new Godot.Collections.Array<Rid>();
        //foreach (var d in activeDice)
         //   exclude.Add(d.GetRid());

        var query = PhysicsRayQueryParameters3D.Create(this.GlobalPosition, this.GlobalPosition + Vector3.Down * 100f);
        query.Exclude = exclude;

        var hit = this.GetWorld3D().DirectSpaceState.IntersectRay(query);

        float floorY = hit.Count > 0 ? ((Vector3)hit["position"]).Y : 0;

        this.Transform = GetUprightTransform(roll, floorY);

        return roll;
    }
}