using Godot;

public partial class Revenant : Enemy
{
	public Revenant()
	{
		DisplayName = "Revenant";
		MaxHealth = 50;
	}

	protected override string SpriteScenePath => "res://Scenes/EnemiesAnim/Revenant.tscn";
	protected override Vector2 SpritePosition => new Vector2(960, 320);
}
