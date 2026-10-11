using Godot;

public partial class StartMenu : Control
{
    [Export] private Button startButton;
    [Export] private bool autoStart = false; // check to skip the menu and go straight into the first fight

    public override void _Ready()
    {
        if (autoStart)
        {
            Callable.From(StartRun).CallDeferred();
            return;
        }

        startButton.Pressed += StartRun;
    }

    private void StartRun()
	{
		if (startButton != null) startButton.Disabled = true; // no double starts
		Run.BeginRun();
	}
}
