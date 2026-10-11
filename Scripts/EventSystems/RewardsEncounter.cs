using Godot;
using System;
using System.Collections.Generic;

public partial class RewardsEncounter : Node2D
{
    [Export] private Container newCardsRow;   // the Revenant's offered cards
    [Export] private Container playerCardRow; // your current cards
    [Export] private Button swapButton;       // "Replace"
    [Export] private Button skipButton;       // "Keep my cards"
    [Export] private Label promptLabel;       // optional

    private readonly List<string> offers = new();
    private readonly List<Card> offerCards = new();
    private readonly List<Card> deckCards = new();
    private int selectedOffer = -1;
    private int selectedDeck = -1;
    private bool finished;

    public override void _Ready()
    {
        if (!Run.IsActive) Run.StartNewRun(); // lets you test this scene on its own (F6)

        // Remove the placeholder cards used for laying out the scene in the editor
        ClearRow(newCardsRow);
        ClearRow(playerCardRow);

        offers.AddRange(Run.RollOffers());
        foreach (string path in offers) offerCards.Add(AddCard(newCardsRow, path, OnOfferClicked));
        foreach (string path in Run.Deck) deckCards.Add(AddCard(playerCardRow, path, OnDeckClicked));

        swapButton.Pressed += OnSwapPressed;
        skipButton.Pressed += Finish;
        UpdateUi();
    }

    private static void ClearRow(Container row)
    {
        foreach (Node child in row.GetChildren())
        {
            row.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Card AddCard(Container row, string path, Action<Card> onClick)
    {
        Card card = GD.Load<PackedScene>(path).Instantiate<Card>();
        card.IsPlayerCard = false; // display only here; never accepts dice
        row.AddChild(card);

        // Children ignore the mouse so the click reaches the card itself
        IgnoreMouse(card);
        card.MouseFilter = Control.MouseFilterEnum.Stop;
        card.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                onClick(card);
        };
        return card;
    }

    private void OnOfferClicked(Card card)
    {
        if (finished) return;
        int i = offerCards.IndexOf(card);
        selectedOffer = selectedOffer == i ? -1 : i; // click again to deselect
        UpdateUi();
    }

    private void OnDeckClicked(Card card)
    {
        if (finished) return;
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
            promptLabel.Text = "Choose one of your cards to replace.";
        else
            promptLabel.Text = $"Replace {deckCards[selectedDeck].CardName} with {offerCards[selectedOffer].CardName}?";
    }

    private void OnSwapPressed()
    {
        if (finished || selectedOffer < 0 || selectedDeck < 0) return;
        Run.SwapCard(selectedDeck, offers[selectedOffer]); // new card takes the old card's slot
        Finish();
    }

    // One swap (or a skip) per visit, then on to the next fight
    private void Finish()
    {
        if (finished) return;
        finished = true;

        swapButton.Disabled = true;
        skipButton.Disabled = true;
        Run.ContinueFromReward();
    }

    private static void IgnoreMouse(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
            IgnoreMouse(child);
        }
    }
}