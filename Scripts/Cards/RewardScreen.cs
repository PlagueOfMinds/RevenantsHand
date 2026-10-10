using Godot;
using System.Collections.Generic;

public partial class RewardScreen : Control
{
    [Export] private Container pairRow;    // one column per deck slot: offer / swap / your card
    [Export] private Button skipButton;
    [Export] private Label promptLabel;    // optional
    [Export] private int columnSeparation = 16;

    private const string DefaultPrompt = "The Revenant offers a trade. Swap one card, or keep your hand.";

    private readonly List<Button> swapButtons = new();
    private bool finished;

    public override void _Ready()
    {
        if (!Run.IsActive) Run.StartNewRun(); // lets you test this scene on its own (F6)

        List<string> offers = Run.RollOffers();

        // Offer i is paired with deck slot i
        for (int i = 0; i < Run.Deck.Count; i++)
        {
            string offerPath = i < offers.Count ? offers[i] : null;
            pairRow.AddChild(BuildColumn(i, offerPath));
        }

        skipButton.Pressed += Finish;
        SetPrompt(DefaultPrompt);
    }

    private Control BuildColumn(int deckIndex, string offerPath)
    {
        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        column.AddThemeConstantOverride("separation", columnSeparation);

        Card yourCard = LoadCard(Run.Deck[deckIndex]);

        if (offerPath == null)
        {
            // Nothing offered for this slot: invisible placeholders keep the columns lined up
            column.AddChild(new Control { CustomMinimumSize = yourCard.CustomMinimumSize });
            column.AddChild(new Button
            {
                Text = "Swap",
                Disabled = true,
                Modulate = new Color(1f, 1f, 1f, 0f),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            });
            column.AddChild(yourCard);
            return column;
        }

        Card offeredCard = LoadCard(offerPath);

        var swap = new Button { Text = "Swap", SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        swap.Pressed += () => OnSwapPressed(deckIndex, offerPath);
        swap.MouseEntered += () =>
        {
            offeredCard.SetHover(true);
            yourCard.SetHover(true);
            SetPrompt($"Swap {yourCard.CardName} for {offeredCard.CardName}?");
        };
        swap.MouseExited += () =>
        {
            offeredCard.SetHover(false);
            yourCard.SetHover(false);
            SetPrompt(DefaultPrompt);
        };
        swapButtons.Add(swap);

        column.AddChild(offeredCard);
        column.AddChild(swap);
        column.AddChild(yourCard);
        return column;
    }

    private Card LoadCard(string path)
    {
        Card card = GD.Load<PackedScene>(path).Instantiate<Card>();
        IgnoreMouse(card); // cards are display-only here; only the buttons take clicks
        return card;
    }

    private void OnSwapPressed(int deckIndex, string offerPath)
    {
        if (finished) return;
        Run.SwapCard(deckIndex, offerPath); // new card takes the old card's slot
        Finish();
    }

    // Swap or skip: lock the screen and move on to the next fight
    private void Finish()
    {
        if (finished) return;
        finished = true;

        foreach (Button button in swapButtons) button.Disabled = true;
        skipButton.Disabled = true;

        Run.ContinueFromReward();
    }

    private void SetPrompt(string text)
    {
        if (promptLabel != null) promptLabel.Text = text;
    }

    private static void IgnoreMouse(Control control)
    {
        control.MouseFilter = MouseFilterEnum.Ignore;
        foreach (Node child in control.GetChildren())
            if (child is Control c) IgnoreMouse(c);
    }
}