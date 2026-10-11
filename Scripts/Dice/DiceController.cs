using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

public partial class DiceController : Node3D
{
    [Export] private PackedScene diceScene;
    [Export] private Node3D diceParent;
    [Export] private Marker3D spawnPoint;
    [Export] private Node3D throwTarget;
    [Export] private Camera3D trayCamera;          // orthographic tray camera (auto-found if left empty)
    [Export] private int diceCount = 3;
    [Export] private float spawnSpacing = 0.6f;
    [Export] private float throwForceMin = 1.5f;
    [Export] private float throwForceMax = 3f;
    [Export] private float throwSpread = 0.3f;
    [Export] private float spinForceMin = 8f;
    [Export] private float spinForceMax = 20f;
    [Export] private float settleThreshold = 0.05f;
    [Export] private float settleTime = 0.5f;

	[Export] private float straightenTime = 0.25f; // seconds to turn upright; 0 = instant

	private Transform3D[] uprightTargets = Array.Empty<Transform3D>();

    [Signal] public delegate void DiceRolledEventHandler(int[] results);

    public int[] LastResults { get; private set; } = Array.Empty<int>();
    public Camera3D TrayCamera => trayCamera ??= GetViewport().GetCamera3D();

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

    public async Task<Dice[]> rollDice()
    {
        if (rolling) return Array.Empty<Dice>();
        ClearDice();

        Vector3 baseDir = throwTarget.GlobalPosition - spawnPoint.GlobalPosition;
        baseDir.Y = 0f;
        baseDir = baseDir.Normalized();
        Vector3 side = Vector3.Up.Cross(baseDir).Normalized();

        for (int i = 0; i < diceCount; i++)
        {
            var dice = diceScene.Instantiate<Dice>();
            diceParent.AddChild(dice);

            float offset = (i - (diceCount - 1) / 2f) * spawnSpacing;
            dice.GlobalPosition = spawnPoint.GlobalPosition + side * offset;
            dice.GlobalRotation = new Vector3(
                rng.RandfRange(0f, Mathf.Tau),
                rng.RandfRange(0f, Mathf.Tau),
                rng.RandfRange(0f, Mathf.Tau));

            Vector3 toTarget = throwTarget.GlobalPosition - dice.GlobalPosition;
            toTarget.Y = 0f;
            Vector3 spread = new Vector3(
                rng.RandfRange(-throwSpread, throwSpread), 0f,
                rng.RandfRange(-throwSpread, throwSpread));
            Vector3 dir = (toTarget.Normalized() + spread).Normalized();
            dice.LinearVelocity = dir * rng.RandfRange(throwForceMin, throwForceMax);

            Vector3 spinAxis = new Vector3(
                rng.RandfRange(-1f, 1f),
                rng.RandfRange(-1f, 1f),
                rng.RandfRange(-1f, 1f)).Normalized();
            dice.AngularVelocity = spinAxis * rng.RandfRange(spinForceMin, spinForceMax);

            activeDice.Add(dice);
        }

        settledTimer = 0f;
        rolling = true;

        await ToSignal(this, SignalName.DiceRolled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await StraightenDice();

		return activeDice.ToArray();
    }

    private void ThrowDie(Dice dice, Vector3 position)
    {
        dice.GlobalPosition = position;
        dice.GlobalRotation = new Vector3(
            rng.RandfRange(0f, Mathf.Tau),
            rng.RandfRange(0f, Mathf.Tau),
            rng.RandfRange(0f, Mathf.Tau));

        Vector3 toTarget = throwTarget.GlobalPosition - dice.GlobalPosition;
        toTarget.Y = 0f;
        Vector3 spread = new Vector3(
            rng.RandfRange(-throwSpread, throwSpread), 0f,
            rng.RandfRange(-throwSpread, throwSpread));
        Vector3 dir = (toTarget.Normalized() + spread).Normalized();
        dice.LinearVelocity = dir * rng.RandfRange(throwForceMin, throwForceMax);

        Vector3 spinAxis = new Vector3(
            rng.RandfRange(-1f, 1f),
            rng.RandfRange(-1f, 1f),
            rng.RandfRange(-1f, 1f)).Normalized();
        dice.AngularVelocity = spinAxis * rng.RandfRange(spinForceMin, spinForceMax);
    }

    public async Task<Dice> createNewDice()
    {
        var dice = diceScene.Instantiate<Dice>();
        diceParent.AddChild(dice);
        ThrowDie(dice, spawnPoint.GlobalPosition);
        activeDice.Add(dice);

        // Wait for THIS die to settle
        float timer = 0f;
        while (timer < settleTime)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            if (!IsInstanceValid(dice)) return null;

            bool moving = dice.LinearVelocity.Length() > settleThreshold ||
                          dice.AngularVelocity.Length() > settleThreshold;
            timer = moving ? 0f : timer + (float)GetPhysicsProcessDeltaTime();
        }

        int result = dice.getRoll();
        Transform3D target = dice.GetUprightTransform(result, FindFloorY(dice));
        await StraightenOne(dice, target);

        return dice;
    }

    private async Task StraightenOne(Dice die, Transform3D target)
    {
        die.Freeze = true;

        if (straightenTime <= 0f)
        {
            die.GlobalTransform = target;
            return;
        }

        Tween tween = CreateTween();
        tween.TweenProperty(die, "global_transform", target, straightenTime)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(tween, Tween.SignalName.Finished);
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
		uprightTargets = new Transform3D[activeDice.Count];

		for (int i = 0; i < activeDice.Count; i++)
		{
			LastResults[i] = activeDice[i].getRoll();
			uprightTargets[i] = activeDice[i].GetUprightTransform(LastResults[i], FindFloorY(activeDice[i]));
		}

		EmitSignal(SignalName.DiceRolled, LastResults);
    }

    // Where a die appears on the 2D page, in the same space as Control.GlobalPosition
    public Vector2 GetCanvasPosition(Dice die)
    {
        var viewport = (SubViewport)GetViewport();
        var container = viewport.GetParent<SubViewportContainer>();

        Vector2 viewportPos = TrayCamera.UnprojectPosition(die.GlobalPosition);
        Vector2 scale = container.Size / (Vector2)viewport.Size;
        return container.GetGlobalTransform() * (viewportPos * scale);
    }

    // How many screen pixels one world unit takes up in the tray view
    public float GetScreenPixelsPerUnit()
    {
        var container = GetViewport().GetParent<SubViewportContainer>();
        Vector2 screenSize = container.Size * container.GetGlobalTransform().Scale;
        float axis = TrayCamera.KeepAspect == Camera3D.KeepAspectEnum.Height ? screenSize.Y : screenSize.X;
        return axis / TrayCamera.Size;
    }

    private void ClearDice()
    {
        foreach (var dice in activeDice)
            if (IsInstanceValid(dice)) dice.QueueFree();
        activeDice.Clear();
    }

	private async Task StraightenDice()
	{
		foreach (var die in activeDice)
			die.Freeze = true; // stop physics from fighting the tween

		if (straightenTime <= 0f)
		{
			for (int i = 0; i < activeDice.Count; i++)
				activeDice[i].GlobalTransform = uprightTargets[i];
			return;
		}

		Tween tween = CreateTween().SetParallel(true);
		for (int i = 0; i < activeDice.Count; i++)
		{
			tween.TweenProperty(activeDice[i], "global_transform", uprightTargets[i], straightenTime)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	// Height of the tray floor under a die, ignoring the dice themselves
	private float? FindFloorY(Dice die)
	{
		var exclude = new Godot.Collections.Array<Rid>();
		foreach (var d in activeDice)
			exclude.Add(d.GetRid());

		var query = PhysicsRayQueryParameters3D.Create(die.GlobalPosition, die.GlobalPosition + Vector3.Down * 100f);
		query.Exclude = exclude;

		var hit = die.GetWorld3D().DirectSpaceState.IntersectRay(query);
		return hit.Count > 0 ? ((Vector3)hit["position"]).Y : null;
	}


    public void RemoveDie(Dice die)
    {
        activeDice.Remove(die);
        if (IsInstanceValid(die)) die.QueueFree();
    }
}