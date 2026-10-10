using Godot;
using System;
using System.Collections.Generic;

public partial class RewardScreen : Control
{
    [Export] private Container offerRow;  // the Revenant's offered cards
    [Export] private Container deckRow;   // your current cards
    [Export] private Button swapButton;
    [Export] private Button skipButton;
    [Export] private Label promptLabel;   // optional

    private readonly List<string> offers = new();
    private readonly List<Card> offerCards = new();
    private readonly List<Card> deckCards = new();
    private int selectedOffer = -1;
    private int selectedDeck = -1;

    public override void _Ready()
    {
        if (!Run.IsActive) Run.StartNewRun(); // lets you test this scene on its own (F6)

        offers.AddRange(Run.RollOffers());
        foreach (string path in offers) offerCards.Add(AddCard(offerRow, path, OnOfferClicked));
        foreach (string path in Run.Deck) deckCards.Add(AddCard(deckRow, path, OnDeckClicked));

        swapButton.Pressed += OnSwapPressed;
        skipButton.Pressed += OnSkipPressed;
        UpdateUi();
    }

    private Card AddCard(Container row, string path, Action<Card> onClick)
    {
        Card card = GD.Load<PackedScene>(path).Instantiate<Card>();
        row.AddChild(card);

        // Children ignore the mouse so the click reaches the card itself
        IgnoreMouse(card);
        card.MouseFilter = MouseFilterEnum.Stop;
        card.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                onClick(card);
        };
        return card;
    }

    private void OnOfferClicked(Card card)
    {
        int i = offerCards.IndexOf(card);
        selectedOffer = selectedOffer == i ? -1 : i; // click again to deselect
        UpdateUi();
    }

    private void OnDeckClicked(Card card)
    {
        int i = deckCards.IndexOf(card);
        selectedDeck = selectedDeck == i ? -1 : i;
        UpdateUi();
    }

    private void UpdateUi()
    {
        for (int i = 0; i < offerCards.Count; i++) offerCards[i].SetHover(i == selectedOffer);
        for (int i = 0; i < deckCards.Count; i++) deckCards[i].SetHover(i == selectedDeck);

        swapButton.Disabled = selectedOffer < 0 || selectedDeck < 0;

        if (promptLabel == null) return;
        if (offerCards.Count == 0)
            promptLabel.Text = "The Revenant has nothing to offer.";
        else if (selectedOffer < 0)
            promptLabel.Text = "Choose a card the Revenant offers.";
        else if (selectedDeck < 0)
            promptLabel.Text = "Choose one of your cards to give up.";
        else
            promptLabel.Text = $"Swap {deckCards[selectedDeck].CardName} for {offerCards[selectedOffer].CardName}?";
    }

    private void OnSwapPressed()
    {
        if (selectedOffer < 0 || selectedDeck < 0) return;
        Run.SwapCard(selectedDeck, offers[selectedOffer]);
        Continue();
    }

    private void OnSkipPressed() => Continue();

    private void Continue()
    {
        swapButton.Disabled = true;
        skipButton.Disabled = true;
        Run.ContinueFromReward();
    }

    private static void IgnoreMouse(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control) control.MouseFilter = MouseFilterEnum.Ignore;
            IgnoreMouse(child);
        }
    }
}