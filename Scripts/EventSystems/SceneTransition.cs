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

    // Standard fade to black
    public async void GoTo(string scenePath)
    {
        if (!StartTransition()) return;

        await Fade(1f);
        await ChangeScene(scenePath);
        await Fade(0f);

        EndTransition();
    }

    // Custom transition scene. It needs a child named "AnimationPlayer" with:
    //   "in"  - ends with the screen fully covered (the scene swaps behind it)
    //   "out" - optional, reveals the new scene
    // Falls back to a normal fade if the transition scene is missing or incomplete.
    public async void GoToWith(string transitionScenePath, string scenePath)
    {
        if (!StartTransition()) return;

        if (!ResourceLoader.Exists(transitionScenePath))
        {
            GD.PushWarning($"SceneTransition: {transitionScenePath} not found. Using a fade instead.");
            await FadeTo(scenePath);
            return;
        }

        Node overlay = GD.Load<PackedScene>(transitionScenePath).Instantiate();
        AddChild(overlay);
        var anim = overlay.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        if (anim == null || !anim.HasAnimation("in"))
        {
            GD.PushError($"SceneTransition: {transitionScenePath} needs an AnimationPlayer with an \"in\" animation. Using a fade instead.");
            overlay.QueueFree();
            await FadeTo(scenePath);
            return;
        }

        await PlayAndWait(anim, "in");
        await ChangeScene(scenePath);
        if (anim.HasAnimation("out")) await PlayAndWait(anim, "out");

        overlay.QueueFree();
        EndTransition();
    }

    private async Task FadeTo(string scenePath)
    {
        await Fade(1f);
        await ChangeScene(scenePath);
        await Fade(0f);
        EndTransition();
    }

    private bool StartTransition()
    {
        if (busy) return false;
        busy = true;
        fade.MouseFilter = Control.MouseFilterEnum.Stop; // block clicks during the transition
        return true;
    }

    private void EndTransition()
    {
        fade.MouseFilter = Control.MouseFilterEnum.Ignore;
        busy = false;
    }

    private async Task ChangeScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task PlayAndWait(AnimationPlayer anim, string animation)
    {
        anim.Play(animation);
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
    }

    private async Task Fade(float alpha)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(fade, "modulate:a", alpha, fadeTime);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}