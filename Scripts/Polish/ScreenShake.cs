using Godot;

public static class ScreenShake
{
    // Configurable defaults
    public static float Strength = 12f;   // max pixel offset
    public static float Duration = 0.25f; // seconds

    public static void Shake(Node from, float? strength = null, float? duration = null)
    {
        var camera = from.GetViewport().GetCamera2D();
        if (camera == null) return;

        float power = strength ?? Strength;
        float time = duration ?? Duration;

        var tween = from.CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            float fade = 1f - t; // shake fades out
            camera.Offset = new Vector2(
                (float)GD.RandRange(-power, power),
                (float)GD.RandRange(-power, power)) * fade;
        }), 0f, 1f, time);
        tween.TweenCallback(Callable.From(() => camera.Offset = Vector2.Zero));
    }
}