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

	protected override AiProfile AiProfile => new AiProfile(
		DamageWeight: 1f,
		HealWeight: 0.5f,
		HealUrgency: 2f
	);
	public override string[] CardScenePaths => new[]
	{
		"res://Scenes/Cards/EnemyCards/DontBite.tscn",
		"res://Scenes/Cards/EnemyCards/TheHandThatFeeds.tscn",
		"res://Scenes/Cards/EnemyCards/OnThisQuietNight.tscn",
	};
}
