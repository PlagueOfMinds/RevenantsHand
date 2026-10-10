using Godot;

public partial class Card : Control
{
    public const string GroupName = "dice_card";

    [Export] public string CardName { get; set; } = "Card";
    [Export] private Control dieSlot;      // plain Control marking where the die sits
    [Export] private CanvasItem highlight; // optional, shown while a die hovers over the card

    [Signal] public delegate void DiePlacedEventHandler(Card card, int value);

    public DiceToken PlacedDie { get; private set; }
    public bool IsOccupied => PlacedDie != null && IsInstanceValid(PlacedDie) && !PlacedDie.IsQueuedForDeletion();

    public override void _Ready()
    {
        AddToGroup(GroupName);
        SetHover(false);
    }

    public bool CanAccept(DiceToken token) => !IsOccupied;

    public void SetHover(bool on)
    {
        if (highlight != null) highlight.Visible = on;
    }

    public void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token)) return;

        PlacedDie = token;
        token.LockInto(dieSlot ?? this);

        int value = token.Value;
        EmitSignal(SignalName.DiePlaced, this, value);
        ApplyEffect(value);
    }

    // Placeholder: override in a subclass or replace with your buff/attack logic
    protected virtual void ApplyEffect(int value)
    {
        GD.Print($"{CardName} activated with a {value}");
    }
}