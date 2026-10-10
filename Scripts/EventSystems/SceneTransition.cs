using Godot;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer
{
    public static SceneTransition Instance { get; private set; }

    [Export] private float fadeTime = 0.4f;

    private ColorRect fade;
    private bool busy;

    public override void _Ready()
    {
        Instance = this;
        Layer = 100; // draw above everything

        fade = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(fade);
        fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        fade.Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public async void GoTo(string scenePath)
    {
        if (busy) return;
        busy = true;
        fade.MouseFilter = Control.MouseFilterEnum.Stop; // block clicks during the fade

        await Fade(1f);
        GetTree().ChangeSceneToFile(scenePath);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Fade(0f);

        fade.MouseFilter = Control.MouseFilterEnum.Ignore;
        busy = false;
    }

    private async Task Fade(float alpha)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(fade, "modulate:a", alpha, fadeTime);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}