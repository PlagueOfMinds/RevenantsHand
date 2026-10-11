using Godot;

public partial class StartMenu : Control
{
    [Export] private Button startButton;
    [Export] private Button quitButton;
    [Export] private bool autoStart = false; // check to skip the menu and go straight into the first fight

    public override void _Ready()
    {
        if (autoStart)
        {
            Callable.From(StartRun).CallDeferred();
            return;
        }

        startButton.Pressed += StartRun;
        quitButton.Pressed += Quit;
    }

    private void StartRun()
	{
		if (startButton != null) startButton.Disabled = true; // no double starts
		Run.BeginRun();
	}

    private void Quit()
    {
        GD.Print("Exiting...");
        GetTree().Quit();
    }
}
