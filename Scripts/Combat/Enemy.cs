using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Enemy : Combatant
{
    private readonly Random rng = new();

    // Each enemy overrides this with its scene (any root type)
    protected virtual string SpriteScenePath => null;

    // Where this enemy stands on screen, in pixels from the top-left
    protected virtual Vector2 SpritePosition => Vector2.Zero;

    // Animation to start on spawn; null leaves the scene's own autoplay setting alone
    protected virtual string IdleAnimation => null;

    public Node Visual { get; private set; }          // the instanced scene
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

    // One move per turn. Default: a random enemy card, powered by the total of all its dice
    public virtual QueuedAction ChooseMove(IReadOnlyList<Card> cards, Combatant opponent)
    {
        if (cards.Count == 0 || Dice.Length == 0) return null;

        Card card = cards[rng.Next(cards.Count)];
        return new QueuedAction(this, opponent, card, Dice.Sum());
    }

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