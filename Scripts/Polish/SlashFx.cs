using Godot;

public partial class SlashFx : CanvasLayer
{
    public static SlashFx Instance { get; private set; }

    public Vector2 TargetPosition { get; set; } = new Vector2(600, 300);

    // Set true while debugging: the slash stays on screen for 2 seconds
    public bool DebugHold { get; set; } = false;

    public override void _Ready()
    {
        Instance = this;
        Layer = 1;
        ProcessMode = ProcessModeEnum.Always; // keeps animating even if the game is paused
        GD.Print("SlashFx ready");
    }

    public void Play(float? angleDeg = null, float length = 360f, float width = 5f,
                 Color? color = null, Vector2? position = null,
                 float curve = 80f, float outline = 3f, float speed = 1.5f)
    {
        Vector2 center = position ?? TargetPosition;
        float angle = angleDeg ?? (float)GD.RandRange(-180.0, 180.0);

        angle = angleDeg ?? (float)(GD.Randf() < 0.5f
            ? GD.RandRange(-65.0, -25.0)    // slashing down-right
            : GD.RandRange(115.0, 155.0));  // slashing down-left

        var slash = new Node2D
        {
            Position = center,
            Rotation = Mathf.DegToRad(angle),
            Scale = new Vector2(0.2f, 1f),
        };

        // Red outline behind, slightly bigger than the white body
        slash.AddChild(MakeBlade(length + outline * 2f, width + outline * 2f,
                                 color ?? new Color(1f, 0.15f, 0.15f), curve));
        // White body on top
        slash.AddChild(MakeBlade(length, width, Colors.White, curve));
        AddChild(slash);

        float hold = DebugHold ? 2f : 0.05f;

        var tween = CreateTween();
        tween.SetSpeedScale(speed);
        tween.TweenProperty(slash, "scale", Vector2.One, 0.1f)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(hold);
        tween.TweenProperty(slash, "modulate:a", 0f, 0.15f);
        tween.TweenCallback(Callable.From(slash.QueueFree));
    }

    private static Polygon2D MakeBlade(float length, float width, Color color, float curve)
    {
        const int steps = 16;
        var pts = new Vector2[(steps + 1) * 2];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = (t - 0.5f) * length;
            float half = Mathf.Max(width * 0.5f * Mathf.Sin(t * Mathf.Pi), 0.5f);
            float bend = Mathf.Sin(t * Mathf.Pi) * curve * 0.5f; // arc, peaks in the middle
            pts[i] = new Vector2(x, bend - half);
            pts[pts.Length - 1 - i] = new Vector2(x, bend + half);
        }
        return new Polygon2D { Polygon = pts, Color = color };
    }
}