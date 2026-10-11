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

	protected override AiProfile AiProfile => new AiProfile(DamageWeight: 1f);
	
	public override string[] CardScenePaths => new[]
    {
        "res://Scenes/Cards/Enemy/JustATouch.tscn",
        "res://Scenes/Cards/Enemy/Mesmerize.tscn",
        "res://Scenes/Cards/Enemy/Embrace.tscn",
    };
}
