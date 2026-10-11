using Godot;

public partial class Goat : Enemy
{
	public Goat()
	{
		DisplayName = "Goat";
		MaxHealth = 20;
	}

	protected override string SpriteScenePath => "res://Scenes/EnemiesAnim/Goat.tscn";
	protected override Vector2 SpritePosition => new Vector2(960, 320);

    protected override AiProfile AiProfile => new AiProfile(DamageWeight: 1f);
	
	public override string[] CardScenePaths => new[]
    {
        "res://Scenes/Cards/EnemyCards/BAHH.tscn",
        "res://Scenes/Cards/EnemyCards/Rage.tscn",
        "res://Scenes/Cards/EnemyCards/BAHH.tscn",
    };
}
