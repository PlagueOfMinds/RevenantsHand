using Godot;
using System.Collections.Generic;

public partial class Enemy : Combatant
{
    // Each enemy overrides this with its scene (any root type)
    protected virtual string SpriteScenePath => null;

    // Where this enemy stands on screen, in pixels from the top-left
    protected virtual Vector2 SpritePosition => Vector2.Zero;

    // Animation to start on spawn; null leaves the scene's own autoplay setting alone
    protected virtual string IdleAnimation => null;

    // This enemy's cards, left to right (the same scene can appear more than once)
    public virtual string[] CardScenePaths => System.Array.Empty<string>();

    // How many cards this enemy plays per turn (default: as many as it can)
    protected virtual int MaxMovesPerTurn => int.MaxValue;

    // How this enemy weighs damage, healing and poison
    protected virtual AiProfile AiProfile => AiProfile.Default;

    public Node Visual { get; private set; }             // the instanced scene
    public AnimatedSprite2D Sprite { get; private set; } // first AnimatedSprite2D inside it, if any

    public override void _Ready()
    {
        Position = SpritePosition;

        if (SpriteScenePath == null) return;

        Visual = GD.Load<PackedScene>(SpriteScenePath).Instantiate();
        AddChild(Visual);

        // Put the scene's origin on the enemy's point; everything inside keeps its layout
        if (Visual is Node2D visual2D) visual2D.Position = Vector2.Zero;
        else if (Visual is Control visualControl) visualControl.Position = Vector2.Zero;

        Sprite = FindSprite(Visual);
        if (IdleAnimation != null) PlayAnimation(IdleAnimation);
    }

    // For later: hurt/attack animations from card effects
    public bool PlayAnimation(string animation)
    {
        if (Sprite?.SpriteFrames == null || !Sprite.SpriteFrames.HasAnimation(animation))
            return false;

        Sprite.Play(animation);
        return true;
    }

    // Shared AI for every enemy, tuned by its AiProfile
    public virtual List<QueuedAction> ChooseMoves(IReadOnlyList<Card> cards, Combatant opponent) =>
        EnemyAI.PlanTurn(this, opponent, cards, MaxMovesPerTurn, AiProfile);

    private static AnimatedSprite2D FindSprite(Node node)
    {
        if (node is AnimatedSprite2D sprite) return sprite;

        foreach (Node child in node.GetChildren())
        {
            AnimatedSprite2D found = FindSprite(child);
            if (found != null) return found;
        }
        return null;
    }
}