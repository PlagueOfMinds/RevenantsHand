using Godot;

public static class ScreenFlash
{
    // Configurable defaults (alpha in the color controls how strong the tint is)
    public static Color HealColor = new Color(0.6f, 0.2f, 1f, 0.4f);
    public static Color DamageColor = new Color(1f, 0.1f, 0.1f, 0.4f);
    public static float Duration = 0.4f;     // seconds to fade out
    public static int DamageThreshold = 2;   // min damage that triggers the red flash

    private static ColorRect rect;
    private static Tween tween;

    public static void Flash(Node from, Color color, float? duration = null)
    {
        // Create the overlay once, on top of everything
        if (!GodotObject.IsInstanceValid(rect))
        {
            var layer = new CanvasLayer { Layer = 100 };
            rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
            rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            layer.AddChild(rect);
            from.GetTree().Root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        tween?.Kill(); // restart cleanly if a flash is already playing
        rect.Color = color;
        rect.Modulate = Colors.White;
        tween = from.GetTree().CreateTween();
        tween.TweenProperty(rect, "modulate:a", 0f, duration ?? Duration);
    }
}