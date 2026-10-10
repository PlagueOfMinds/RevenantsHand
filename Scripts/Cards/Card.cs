using Godot;
using System.Threading.Tasks;

public partial class Card : Control
{
    public const string GroupName = "dice_card";

    [Export] public string CardName { get; set; } = "Card";
    [Export] public bool IsPlayerCard { get; set; } = true; // uncheck for enemy cards
    [Export] private Control dieSlot;
    [Export] private CanvasItem highlight;

    [Signal] public delegate void DiePlacedEventHandler(Card card, int value);

    public DiceToken PlacedDie { get; private set; }
    public bool IsOccupied => PlacedDie != null && IsInstanceValid(PlacedDie) && !PlacedDie.IsQueuedForDeletion();
    public int PlacedValue => IsOccupied ? PlacedDie.Value : 0;
    public Control DieSlot => dieSlot ?? this;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        SetHover(false);
    }

    public bool CanAccept(DiceToken token) => IsPlayerCard;

    public void SetHover(bool on)
    {
        if (highlight != null) highlight.Visible = on;
    }

    public void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || (IsOccupied && PlacedDie != token)) return;

        PlacedDie = token;
        token.AttachTo(this);
        EmitSignal(SignalName.DiePlaced, this, token.Value);
    }

    public void ReleaseDie() => PlacedDie = null;

    // The card's effect. Card-type subclasses (AttackCard, DefendCard, HealCard...) override this.
    // user = whoever played the card, opponent = the other side. A heal card heals user, an attack hits opponent.
    public virtual Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"{user.Name} used {CardName} with a {value} (no effect yet)");
        return Task.CompletedTask;
    }
}