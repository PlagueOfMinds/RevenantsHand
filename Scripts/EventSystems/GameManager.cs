using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GameManager : Node
{
    public enum Phase { Roll, Rolling, Place, Resolve, GameOver }

    [ExportGroup("Dice")]
    [Export] private DiceController playerDice;
    [Export] private DiceController enemyDice;
    [Export] private DiceTokenLayer tokenLayer;

    [ExportGroup("Cards")]
    [Export] private Control playerCardRow;
    [Export] private Control enemyCardRow;

    [ExportGroup("UI")]
    [Export] private Button rollButton;
    [Export] private Button lockInButton;
    [Export] private Label turnLabel;
    [Export] private Label playerHealthLabel; // optional
    [Export] private Label enemyHealthLabel;  // optional
    [Export] private float fadedAlpha = 0.35f;
    [Export] private float actionDelay = 0.6f;      // pause between actions so each one is readable
    [Export] private float endOfBattleDelay = 2f;   // how long "Victory"/"Defeat" shows before moving on

    [Signal] public delegate void BattleStartedEventHandler(Enemy enemy);
    [Signal] public delegate void GameEndedEventHandler(bool playerWon);

    public Player Player { get; private set; }
    public Enemy Enemy { get; private set; }
    public int Turn { get; private set; } = 1;
    public Phase CurrentPhase { get; private set; }

    public override void _Ready()
    {
        if (!Run.IsActive) Run.StartNewRun(); // running Combat directly (F6) still works

        Player = new Player { MaxHealth = Run.PlayerMaxHealth };
        Player.SetHealth(Run.PlayerHealth);
        AddChild(Player);
        Player.Changed += OnCombatantChanged;

        rollButton.Pressed += OnRollPressed;
        lockInButton.Pressed += OnLockInPressed;

        Callable.From(() => StartBattle(Encounter.CreateEnemy())).CallDeferred();
    }

    public void StartBattle(Enemy enemy)
    {
        if (Enemy != null)
        {
            Enemy.Changed -= OnCombatantChanged;
            Enemy.QueueFree(); // remove the previous enemy and its sprite
        }

        Enemy = enemy;
        AddChild(Enemy); // runs Enemy._Ready(): positions it and loads its sprite
        Enemy.Changed += OnCombatantChanged;

        BuildPlayerCards();

        Turn = 1;
        tokenLayer.ClearTokens();
        UpdateTurnLabel();
        UpdateHealthLabels();
        SetPhase(Phase.Roll);
        EmitSignal(SignalName.BattleStarted, Enemy);
    }

    // Fill the CardRow from the run's deck, left to right
    private void BuildPlayerCards()
    {
        foreach (Node child in playerCardRow.GetChildren())
        {
            playerCardRow.RemoveChild(child);
            child.QueueFree();
        }

        foreach (string path in Run.Deck)
            playerCardRow.AddChild(GD.Load<PackedScene>(path).Instantiate<Card>());
    }

    private void OnCombatantChanged(Combatant _) => UpdateHealthLabels();

    // Stage 1: player and enemy roll at the same time
    private async void OnRollPressed()
    {
        if (CurrentPhase != Phase.Roll) return;
        SetPhase(Phase.Rolling);

        Task<Dice[]> playerRoll = playerDice.rollDice();
        Task<Dice[]> enemyRoll = enemyDice.rollDice();
        await Task.WhenAll(playerRoll, enemyRoll);

        Player.SetDice(playerDice.LastResults);
        Enemy.SetDice(enemyDice.LastResults);
        tokenLayer.CreateTokens(playerRoll.Result, playerDice);

        // Stage 2: player drags dice onto cards
        SetPhase(Phase.Place);
    }

    // Stages 3-5: lock in, build the queue, execute it, advance the turn
    private async void OnLockInPressed()
    {
        if (CurrentPhase != Phase.Place) return;
        SetPhase(Phase.Resolve);

        var queue = new List<QueuedAction>();

        // Player: every card with a die on it, left to right by position in the CardRow
        foreach (Card card in GetCards(playerCardRow))
        {
            if (card.IsOccupied)
                queue.Add(new QueuedAction(Player, Enemy, card, card.PlacedValue));
        }

        // Enemy: a single move
        QueuedAction enemyMove = Enemy.ChooseMove(GetCards(enemyCardRow), Player);
        if (enemyMove != null) queue.Add(enemyMove);

        foreach (QueuedAction action in queue)
        {
            if (Player.IsDead || Enemy.IsDead) break;

            action.Card.SetHover(true);
            await action.Card.Resolve(action.User, action.Opponent, action.Value);
            await Wait(actionDelay);
            action.Card.SetHover(false);
        }

        if (Player.IsDead || Enemy.IsDead)
        {
            EndGame();
            return;
        }

        NextTurn();
    }

    private void NextTurn()
    {
        tokenLayer.ClearTokens();
        Player.StartTurn();
        Enemy.StartTurn();

        Turn++;
        UpdateTurnLabel();
        SetPhase(Phase.Roll);
    }

    private async void EndGame()
    {
        SetPhase(Phase.GameOver);
        bool playerWon = Enemy.IsDead;
        turnLabel.Text = playerWon ? "Victory" : "Defeat";
        EmitSignal(SignalName.GameEnded, playerWon);

        await Wait(endOfBattleDelay);

        if (playerWon) Run.BattleWon(Player.Health);
        else Run.BattleLost();
    }

    private void SetPhase(Phase phase)
    {
        CurrentPhase = phase;
        SetButtonActive(rollButton, phase == Phase.Roll);
        SetButtonActive(lockInButton, phase == Phase.Place);
        tokenLayer.SetInteractable(phase == Phase.Place);
    }

    private void SetButtonActive(Button button, bool active)
    {
        button.Disabled = !active;
        button.CreateTween().TweenProperty(button, "modulate:a", active ? 1f : fadedAlpha, 0.25f);
    }

    private void UpdateTurnLabel() => turnLabel.Text = $"Turn {Turn}";

    private void UpdateHealthLabels()
    {
        if (playerHealthLabel != null && Player != null) playerHealthLabel.Text = Describe(Player);
        if (enemyHealthLabel != null && Enemy != null) enemyHealthLabel.Text = Describe(Enemy);
    }

    private static string Describe(Combatant c) =>
        c.Block > 0
            ? $"{c.DisplayName}: {c.Health}/{c.MaxHealth} HP (+{c.Block} block)"
            : $"{c.DisplayName}: {c.Health}/{c.MaxHealth} HP";

    private static List<Card> GetCards(Node row)
    {
        var cards = new List<Card>();
        foreach (Node child in row.GetChildren())
            if (child is Card card) cards.Add(card);
        return cards;
    }

    private async Task Wait(float seconds)
    {
        if (seconds > 0f)
            await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}