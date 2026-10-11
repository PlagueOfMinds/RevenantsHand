using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

// What a card does, so the enemy AI (and later, tooltips) can reason about it
public enum CardEffect { None, Damage, Heal, Poison }

// One effect a card produces for a given die
public readonly record struct CardOutcome(CardEffect Effect, int Amount);

public partial class Card : Control
{
    public const string GroupName = "dice_card";

    [Export] public string CardName { get; set; } = "Card";
    [Export] public bool IsPlayerCard { get; set; } = true; // uncheck for enemy cards
    [Export] private Control dieSlot;
    [Export] private CanvasItem highlight;
    [Export] public AudioStream ResolveSound { get; set; }
    public bool disabled = false;
    public int turnCount = 0;

    [Signal] public delegate void DiePlacedEventHandler(Card card, int value);

    public DiceToken PlacedDie { get; private set; }
    public bool IsOccupied => PlacedDie != null && IsInstanceValid(PlacedDie) && !PlacedDie.IsQueuedForDeletion();
    public int PlacedValue => IsOccupied ? PlacedDie.Value : 0;
    public Control DieSlot => dieSlot ?? this;

    public GameManager gameManager;

    private Tween hoverTween;
    private bool hovering;
    private const float HoverScale = 1.05f;
    private const float MaxTilt = 0.08f; // radians (~8 degrees)

    public void SetHover(bool on)
    {
        if (highlight != null) highlight.Visible = on;
        hovering = on;

        hoverTween?.Kill();
        hoverTween = CreateTween()
            .SetParallel()
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        hoverTween.TweenProperty(this, "scale", on ? Vector2.One * HoverScale : Vector2.One, 0.15);
        if (!on)
            hoverTween.TweenProperty(this, "rotation", 0f, 0.2);
    }

    // ---- What the card does: override these in each card subclass ----

    // Simple cards: one kind of effect...
    public virtual CardEffect Effect => CardEffect.None;

    // ...with this strength for a die value X (e.g. X-2, 2X, or a fixed 12)
    public virtual int GetAmount(int dieValue) => dieValue;

    // Which die values the card takes (e.g. "4 or greater", "odd", "only 6")
    public virtual bool AcceptsValue(int dieValue) => true;

    // Conditions on the battle (e.g. "only if took 10 or more last turn")
    public virtual bool CanUse(Combatant user, Combatant opponent) => true;

    // Everything the card does for a die value. Override for cards with several effects.
    public virtual IEnumerable<CardOutcome> GetEffects(int dieValue)
    {
        if (Effect == CardEffect.None) yield break;

        int amount = GetAmount(dieValue);
        if (amount > 0) yield return new CardOutcome(Effect, amount);
    }

    public override void _Ready()
    {
        PivotOffset = Size / 2;
        Resized += () => PivotOffset = Size / 2;


        if (GameManager.ImgManager != null)
        {
            gameManager = GameManager.ImgManager;
            turnCount = gameManager.Turn;
        }
        AddToGroup(GroupName);
        SetHover(false);
    }

    public override void _Process(double delta)
    {
        if (hovering)
        {
            Vector2 half = Size / 2;
            Vector2 offset = (GetLocalMousePosition() - half) / half; // roughly -1..1
            float target = Mathf.Clamp(offset.X, -1f, 1f) * MaxTilt;
            Rotation = Mathf.Lerp(Rotation, target, 10f * (float)delta);
        }


        if (gameManager != null && turnCount != gameManager.Turn)
        {
            turnCount = gameManager.Turn;
            if (disabled)
            {
                flipInputBlocking();
                disabled = false;
            }
        }

    }

    // Player cards take dice that meet their value condition
    public bool CanAccept(DiceToken token) => IsPlayerCard && AcceptsValue(token.Value);

    /*
    public void SetHover(bool on)
    {
        if (highlight != null) highlight.Visible = on;
    }
    */

    public virtual void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || (IsOccupied && PlacedDie != token)) return;
        PlacedDie = token;
        token.AttachTo(this);
        EmitSignal(SignalName.DiePlaced, this, token.Value);
    }

    public void ReleaseDie() => PlacedDie = null;

    // Applies every effect from GetEffects. user = whoever played the card, opponent = the other side.
    public virtual Task Resolve(Combatant user, Combatant opponent, int value)
    {
        foreach (CardOutcome outcome in GetEffects(value))
        {
            switch (outcome.Effect)
            {
                case CardEffect.Damage: opponent.TakeDamage(outcome.Amount); break;
                case CardEffect.Heal:   user.Heal(outcome.Amount);           break;
                case CardEffect.Poison: opponent.AddPoison(outcome.Amount);  break;
            }
        }
        return Task.CompletedTask;
    }

    public void disableSelf()
    {
        if (!disabled)
        {
            disabled = true;
            flipInputBlocking();
        }
    }

    public virtual void flipInputBlocking()
    {
        MouseFilterEnum targetFilter = disabled ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        SetMouseFilterRecursive(this, targetFilter);
    }

    private static void SetMouseFilterRecursive(Node parent, MouseFilterEnum filter)
    {
        if (parent is Control control)
            control.MouseFilter = filter;

        foreach (Node child in parent.GetChildren())
            SetMouseFilterRecursive(child, filter);
    }


}