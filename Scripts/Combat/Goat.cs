using Godot;

public partial class Goat : Enemy
{
	public Goat()
	{
		DisplayName = "Goat";
		MaxHealth = 50;
	}

	protected override string SpriteScenePath => "res://Scenes/EnemiesAnim/Goat.tscn";
	protected override Vector2 SpritePosition => new Vector2(960, 320);
}
