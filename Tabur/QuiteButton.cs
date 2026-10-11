using Godot;
using System;

public partial class QuiteButton : Button
{
    // This method will handle the click event
    private void OnPressed()
    {
        GD.Print("Exiting...");
        GetTree().Quit();
    }
}