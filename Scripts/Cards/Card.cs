using Godot;
using System.Threading.Tasks;

public partial class Card : Control
{
    public const string GroupName = "dice_card";

    [Export] public string CardName { get; set; } = "Card";
    [Export] public bool IsPlayerCard { get; set; } = true; // uncheck for enemy cards
    [Export] private Control dieSlot;
    [Export] private CanvasItem highlight;
    public bool disabled = false;
    public int turnCount = 0;

    [Signal] public delegate void DiePlacedEventHandler(Card card, int value);

    public DiceToken PlacedDie { get; private set; }
    public bool IsOccupied => PlacedDie != null && IsInstanceValid(PlacedDie) && !PlacedDie.IsQueuedForDeletion();
    public int PlacedValue => IsOccupied ? PlacedDie.Value : 0;

    public GameManager gameManager;

    public override void _Ready()
    {
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
        if (gameManager == null && GameManager.ImgManager != null)
        {
            gameManager = GameManager.ImgManager;
        }
        if(disabled)
        {
            if(turnCount != gameManager.Turn)
            {
                disabled = false;
                flipInputBlocking();
            }
        }
        turnCount = gameManager.Turn;
    }

    public bool CanAccept(DiceToken token) => IsPlayerCard && !IsOccupied;

    public void SetHover(bool on)
    {
        if (highlight != null) highlight.Visible = on;
    }

    public virtual void PlaceDie(DiceToken token)
    {
        if (!CanAccept(token) || disabled) return;

        PlacedDie = token;
        token.LockInto(dieSlot ?? this);
        EmitSignal(SignalName.DiePlaced, this, token.Value);
    }

    // The card's effect. Card-type subclasses (AttackCard, DefendCard, HealCard...) override this.
    // user = whoever played the card, opponent = the other side. A heal card heals user, an attack hits opponent.
    public virtual Task Resolve(Combatant user, Combatant opponent, int value)
    {
        GD.Print($"{user.Name} used {CardName} with a {value} (no effect yet)");
        return Task.CompletedTask;
    }

    public void disableSelf()
    {
        if(!disabled)
        {
            disabled = true;
            flipInputBlocking();
        }
    }

    private void flipInputBlocking()
    {
        // When disabled, ignore input events. When enabled, stop/handle input events.
        MouseFilterEnum targetFilter = disabled ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;

        SetMouseFilterRecursive(this, targetFilter);
        GD.Print("Flipped the filter");
        GD.Print(this.MouseFilter);
    }

    private static void SetMouseFilterRecursive(Node parent, MouseFilterEnum filter)
    {
        if (parent is Control control)
        {
            control.MouseFilter = filter;
        }

        foreach (Node child in parent.GetChildren())
        {
            SetMouseFilterRecursive(child, filter);
        }
    }
}