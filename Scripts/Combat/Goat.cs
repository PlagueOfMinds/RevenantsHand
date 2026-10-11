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

    protected override AiProfile AiProfile => new AiProfile(DamageWeight: 1f);
	
	public override string[] CardScenePaths => new[]
    {
        "res://Scenes/Cards/Enemy/Bahh.tscn",
        "res://Scenes/Cards/Enemy/Rage.tscn",
        "res://Scenes/Cards/Enemy/Bahh.tscn",
    };
}
