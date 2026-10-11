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
	[Export] private Font swapFont;                                // leave empty to use the default below
	[Export] private Color swapFontColor = new Color("#FB3B02");
	[Export] private int swapFontSize = 0;      

    private const string DefaultPrompt = "The Revenant offers a trade. Swap one card, or keep your hand.";
	private const string OpenSoundPath = "res://AudioFX/Music/shopDrums.wav";

    private readonly List<Button> swapButtons = new();
    private bool finished;

    public override void _Ready()
    {
		swapFont ??= GD.Load<Font>("res://Font/Imprint MT Std Shadow.otf"); // Load font for later

        if (!Run.IsActive) Run.StartNewRun(); // lets you test this scene on its own (F6)
		Audio.Instance.PlaySfx(GD.Load<AudioStream>(OpenSoundPath), pitchVariance: 0f); // Audio for shop opening

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
    // [your card] [swap] [offered card], side by side
	private Control BuildPair(int deckIndex, string offerPath)
	{
		var pair = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		pair.AddThemeConstantOverride("separation", pairSeparation);

		Card yourCard = LoadCard(Run.Deck[deckIndex]);

		if (offerPath == null)
		{
			// Nothing offered for this slot: invisible placeholders keep the spacing even
			pair.AddChild(yourCard);
			pair.AddChild(CreateButton(visible: false));
			pair.AddChild(new Control { CustomMinimumSize = yourCard.CustomMinimumSize });
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

		pair.AddChild(yourCard);
		pair.AddChild(swap);
		pair.AddChild(offeredCard);
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

		// Font and color, kept the same when hovered, pressed or focused
		if (swapFont != null) button.AddThemeFontOverride("font", swapFont);
		if (swapFontSize > 0) button.AddThemeFontSizeOverride("font_size", swapFontSize);

		button.AddThemeColorOverride("font_color", swapFontColor);
		button.AddThemeColorOverride("font_hover_color", swapFontColor);
		button.AddThemeColorOverride("font_pressed_color", swapFontColor);
		button.AddThemeColorOverride("font_focus_color", swapFontColor);
		button.AddThemeColorOverride("font_hover_pressed_color", swapFontColor);
		button.AddThemeColorOverride("font_disabled_color", swapFontColor with { A = 0.4f }); // dimmed after a swap

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