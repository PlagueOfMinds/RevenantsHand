using Godot;
using System.Collections.Generic;

public partial class DiceController : Node3D
{
    [Export] private PackedScene diceScene;   // Dice .tscn (root must be the Dice RigidBody3D)
    [Export] private Node3D diceParent;       // where dice are added, inside the SubViewport
    [Export] private Marker3D spawnPoint;     // where dice appear, inside the walls
    [Export] private Node3D throwTarget;      // Marker3D at the center of the tray
    [Export] private int diceCount = 3;
    [Export] private float spawnSpacing = 0.6f;
    [Export] private float throwForceMin = 1.5f;
	[Export] private float throwForceMax = 3f;
	[Export] private float spinForceMin = 8f;
	[Export] private float spinForceMax = 20f;
    [Export] private float throwSpread = 0.3f;
    [Export] private float settleThreshold = 0.05f; // velocity counted as "stopped"
    [Export] private float settleTime = 0.5f;       // how long all dice must stay stopped

    [Signal] public delegate void DiceRolledEventHandler(int[] results);

    public int[] LastResults { get; private set; } = new int[0];

    private readonly List<Dice> activeDice = new();
    private readonly RandomNumberGenerator rng = new();
    private bool rolling;
    private float settledTimer;

    public override void _Ready()
    {
        rng.Randomize();

        if (diceScene == null) GD.PushError("DiceController: diceScene is not assigned.");
        if (diceParent == null) GD.PushError("DiceController: diceParent is not assigned.");
        if (spawnPoint == null) GD.PushError("DiceController: spawnPoint is not assigned.");
        if (throwTarget == null) GD.PushError("DiceController: throwTarget is not assigned.");
    }

    public void rollDice()
    {
        if (rolling) return;
        ClearDice();

        // Flat direction from the spawn point to the target
        Vector3 baseDir = throwTarget.GlobalPosition - spawnPoint.GlobalPosition;
        baseDir.Y = 0f;
        baseDir = baseDir.Normalized();

        // Sideways axis, perpendicular to the throw, used to spread the dice out
        Vector3 side = Vector3.Up.Cross(baseDir).Normalized();

        for (int i = 0; i < diceCount; i++)
        {
            var dice = diceScene.Instantiate<Dice>();
            diceParent.AddChild(dice); // must be in the tree before setting global transforms

            float offset = (i - (diceCount - 1) / 2f) * spawnSpacing;
            dice.GlobalPosition = spawnPoint.GlobalPosition + side * offset;
            dice.GlobalRotation = new Vector3(
                rng.RandfRange(0f, Mathf.Tau),
                rng.RandfRange(0f, Mathf.Tau),
                rng.RandfRange(0f, Mathf.Tau));

            // Aim each die at the target from where it spawned, kept flat, with a little spread
            Vector3 toTarget = throwTarget.GlobalPosition - dice.GlobalPosition;
            toTarget.Y = 0f;
            Vector3 spread = new Vector3(
                rng.RandfRange(-throwSpread, throwSpread), 0f,
                rng.RandfRange(-throwSpread, throwSpread));
            Vector3 dir = (toTarget.Normalized() + spread).Normalized();

            float force = rng.RandfRange(throwForceMin, throwForceMax);
			dice.LinearVelocity = dir * force;

			// Random spin axis with a random strength
			Vector3 spinAxis = new Vector3(
				rng.RandfRange(-1f, 1f),
				rng.RandfRange(-1f, 1f),
				rng.RandfRange(-1f, 1f)).Normalized();
			float spin = rng.RandfRange(spinForceMin, spinForceMax);
			dice.AngularVelocity = spinAxis * spin;

            activeDice.Add(dice);
        }

        settledTimer = 0f;
        rolling = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!rolling) return;

        foreach (var dice in activeDice)
        {
            if (dice.LinearVelocity.Length() > settleThreshold ||
                dice.AngularVelocity.Length() > settleThreshold)
            {
                settledTimer = 0f;
                return;
            }
        }

        settledTimer += (float)delta;
        if (settledTimer < settleTime) return;

        rolling = false;
        LastResults = new int[activeDice.Count];
        for (int i = 0; i < activeDice.Count; i++)
            LastResults[i] = activeDice[i].getRoll();

        EmitSignal(SignalName.DiceRolled, LastResults);
    }

    private void ClearDice()
    {
        foreach (var dice in activeDice)
            dice.QueueFree();
        activeDice.Clear();
    }
}