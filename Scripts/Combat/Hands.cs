using Godot;

public partial class Hands : Enemy
{
	public Hands()
	{
		DisplayName = "Hands";
		MaxHealth = 50;
	}

	protected override string SpriteScenePath => "res://Scenes/EnemiesAnim/Hands.tscn";
	protected override Vector2 SpritePosition => new Vector2(960, 320);
}
