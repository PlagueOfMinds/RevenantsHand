using Godot;
using System.Collections.Generic;

public partial class RewardsEncounter : Node2D
{
    [Export] private Container pairRow;           // one group per pair: offered card, swap button, your card
    [Export] private Button skipButton;           // "Keep my cards"
    [Export] private Label promptLabel;           // optional
    [Export] private PackedScene swapButtonScene; // optional: your own styled button (root must be a Button)
    [Export] private string swapText = "Swap";
    [Export] private Vector2 swapButtonSize = new Vector2(64, 44);
    [Export] private int pairSeparation = 8;      // gap between the cards and the button inside a pair

    private const string DefaultPrompt = "The Revenant offers a trade. Swap one card, or keep your hand.";

    private readonly List<Button> swapButtons = new();
    private bool finished;

    public override void _Ready()
    {
        if (!Run.IsActive) Run.StartNewRun(); // lets you test this scene on its own (F6)

        // Remove any placeholders used for laying out the scene in the editor
        foreach (Node child in pairRow.GetChildren())
        {
            pairRow.RemoveChild(child);
            child.QueueFree();
        }

        List<string> offers = Run.RollOffers();

        // Offer i is paired with your card i
        for (int i = 0; i < Run.Deck.Count; i++)
            pairRow.AddChild(BuildPair(i, i < offers.Count ? offers[i] : null));

        skipButton.Pressed += Finish;
        SetPrompt(DefaultPrompt);
    }

    // [offered card] [swap] [your card], side by side
    private Control BuildPair(int deckIndex, string offerPath)
    {
        var pair = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        pair.AddThemeConstantOverride("separation", pairSeparation);

        Card yourCard = LoadCard(Run.Deck[deckIndex]);

        if (offerPath == null)
        {
            // Nothing offered for this slot: invisible placeholders keep the spacing even
            pair.AddChild(new Control { CustomMinimumSize = yourCard.CustomMinimumSize });
            pair.AddChild(CreateButton(visible: false));
            pair.AddChild(yourCard);
            return pair;
        }

        Card offeredCard = LoadCard(offerPath);
        Button swap = CreateButton(visible: true);

        swap.Pressed += () => OnSwapPressed(deckIndex, offerPath);
        swap.MouseEntered += () =>
        {
            offeredCard.SetHover(true);
            yourCard.SetHover(true);
            SetPrompt($"Replace {yourCard.CardName} with {offeredCard.CardName}?");
        };
        swap.MouseExited += () =>
        {
            offeredCard.SetHover(false);
            yourCard.SetHover(false);
            SetPrompt(DefaultPrompt);
        };
        swapButtons.Add(swap);

        pair.AddChild(offeredCard);
        pair.AddChild(swap);
        pair.AddChild(yourCard);
        return pair;
    }

    private Card LoadCard(string path)
    {
        Card card = GD.Load<PackedScene>(path).Instantiate<Card>();
        card.IsPlayerCard = false;                            // display only here
        card.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; // keep its own width inside the pair
        IgnoreMouse(card);                                    // only the swap buttons take clicks
        return card;
    }

    private Button CreateButton(bool visible)
    {
        Button button = swapButtonScene != null
            ? swapButtonScene.Instantiate<Button>()
            : new Button { Text = swapText };

        button.CustomMinimumSize = swapButtonSize;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; // centered beside the cards

        if (!visible)
        {
            button.Disabled = true;
            button.Modulate = new Color(1f, 1f, 1f, 0f);
        }
        return button;
    }

    private void OnSwapPressed(int deckIndex, string offerPath)
    {
        if (finished) return;
        Run.SwapCard(deckIndex, offerPath); // new card takes the old card's slot
        Finish();
    }

    // One swap (or a skip) per visit, then on to the next fight
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
        control.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach (Node child in control.GetChildren())
            if (child is Control c) IgnoreMouse(c);
    }
}