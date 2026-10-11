using Godot;

public partial class Hands : Enemy
{
	public Hands()
	{
		DisplayName = "Hands";
		MaxHealth = 30;
	}

	protected override string SpriteScenePath => "res://Scenes/EnemiesAnim/Hands.tscn";
	protected override Vector2 SpritePosition => new Vector2(960, 320);

	protected override AiProfile AiProfile => new AiProfile(DamageWeight: 1f);
	
	public override string[] CardScenePaths => new[]
	{
		"res://Scenes/Cards/EnemyCards/JustATouch.tscn",
		"res://Scenes/Cards/EnemyCards/Mesmerize.tscn",
		"res://Scenes/Cards/EnemyCards/Embrace.tscn",
	};
}
