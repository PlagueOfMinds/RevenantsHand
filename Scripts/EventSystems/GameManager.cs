using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
	[Export] private StatLabel playerHealthText;
	[Export] private StatLabel enemyHealthText;
	[Export] private StatLabel playerPoisonText; // optional
	[Export] private StatLabel enemyPoisonText;  // optional
	[Export] private float fadedAlpha = 0.35f;
	[Export] private float actionDelay = 0.6f;      // pause between actions so each one is readable
	[Export] private float endOfBattleDelay = 2f;   // how long "Victory"/"Defeat" shows before moving on

	[Signal] public delegate void BattleStartedEventHandler(Enemy enemy);
	[Signal] public delegate void GameEndedEventHandler(bool playerWon);

	public Player Player { get; private set; }
	public Enemy Enemy { get; private set; }
	public int Turn { get; private set; } = 1;
	public Phase CurrentPhase { get; private set; }

	// The GameManager of the current Combat scene (null outside combat)
	public static GameManager ImgManager { get; private set; }

	private bool BattleOver => Player.IsDead || Enemy.IsDead;

	public override void _Ready()
	{
		ImgManager = this;
		if (!Run.IsActive) Run.StartNewRun(); // running Combat directly (F6) still works

		Player = new Player { MaxHealth = Run.PlayerMaxHealth };
		Player.SetHealth(Run.PlayerHealth);
		AddChild(Player);
		Player.Changed += OnCombatantChanged;
		Player.IsPlayer = true;

		rollButton.Pressed += OnRollPressed;
		lockInButton.Pressed += OnLockInPressed;

		Callable.From(() => StartBattle(Encounter.CreateEnemy())).CallDeferred();
	}

	public override void _ExitTree()
	{
		// Don't leave a reference to a freed GameManager for cards in other scenes
		if (ImgManager == this) ImgManager = null;
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
		BuildEnemyCards();

		//SlashFx.Instance.TargetPosition = enemy.GlobalPosition;
		//SlashFx.Instance.Layer = 1000;
		//SlashFx.Instance.Play(position: new Vector2(500, 300), length: 600f, width: 80f);

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

	// Fill the EnemyCardRow from the current enemy's own cards
	private void BuildEnemyCards()
	{
		foreach (Node child in enemyCardRow.GetChildren())
		{
			enemyCardRow.RemoveChild(child);
			child.QueueFree();
		}

		foreach (string path in Enemy.CardScenePaths)
		{
			Card card = GD.Load<PackedScene>(path).Instantiate<Card>();
			card.IsPlayerCard = false;
			enemyCardRow.AddChild(card);
		}
	}

	private void OnCombatantChanged(Combatant _) => UpdateHealthLabels();

	// Stage 1: player and enemy roll their 3D dice at the same time
	private async void OnRollPressed()
	{
		if (CurrentPhase != Phase.Roll) return;
		SetPhase(Phase.Rolling);

		Task<Dice[]> playerRoll = playerDice.rollDice();
		Task<Dice[]> enemyRoll = enemyDice.rollDice();
		await Task.WhenAll(playerRoll, enemyRoll);

		Player.SetDice(playerDice.LastResults);
		Enemy.SetDice(enemyDice.LastResults); // what the enemy's 3D dice landed on
		tokenLayer.CreateTokens(playerRoll.Result, playerDice);

		// Stage 2: player drags dice onto cards
		SetPhase(Phase.Place);
	}

	// Stages 3-5: lock in, then each side acts and takes its poison at the end of its turn
	private async void OnLockInPressed()
	{
		if (CurrentPhase != Phase.Place) return;
		SetPhase(Phase.Resolve);

		// Player's turn: cards left to right, then poison
		var playerActions = new List<QueuedAction>();
		foreach (Card card in GetCards(playerCardRow))
		{
			if (card.IsOccupied)
				playerActions.Add(new QueuedAction(Player, Enemy, card, card.PlacedValue));
		}

		await ResolveActions(playerActions);
		await EndOfTurnPoison(Player);
		Player.EndOwnTurn();

		// Enemy's turn: the shared AI plans now, after seeing what the player did
		if (!BattleOver)
		{
			List<Card> enemyCards = GetCards(enemyCardRow);
			GD.Print($"[AI] {Enemy.DisplayName} dice: [{string.Join(", ", Enemy.Dice)}] " +
			$"cards: [{string.Join(", ", enemyCards.Select(c => $"{c.CardName} ({c.GetType().Name})"))}]");

			List<QueuedAction> enemyActions = Enemy.ChooseMoves(enemyCards, Player);
			GD.Print($"[AI] chose: [{string.Join(", ", enemyActions.Select(a => $"{a.Card.CardName} with {a.Value}"))}]");

			await ResolveActions(enemyActions);
			await EndOfTurnPoison(Enemy);
			Enemy.EndOwnTurn();
		}

		if (BattleOver)
		{
			EndGame();
			return;
		}

		NextTurn();
	}

	private async Task ResolveActions(IEnumerable<QueuedAction> actions)
	{
		foreach (QueuedAction action in actions)
		{
			if (BattleOver) return;
			if (!action.Card.CanUse(action.User, action.Opponent) || !action.Card.AcceptsValue(action.Value)) continue;

			action.Card.SetHover(true);
			//Audio.Instance.PlaySfx(action.Card.ResolveSound);
			await action.Card.Resolve(action.User, action.Opponent, action.Value);
			await Wait(actionDelay);
			action.Card.SetHover(false);
		}
	}

	private async Task EndOfTurnPoison(Combatant combatant)
	{
		if (BattleOver || combatant.Poison <= 0) return;

		int dealt = combatant.TickPoison();
		GD.Print($"{combatant.DisplayName} takes {dealt} poison damage");
		await Wait(actionDelay); // pause so the health change is readable
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

		playerHealthText?.Display(Player);
		enemyHealthText?.Display(Enemy);
		playerPoisonText?.Display(Player);
		enemyPoisonText?.Display(Enemy);
	}

	private static string Describe(Combatant c)
	{
		string text = $"{c.DisplayName}: {c.Health}/{c.MaxHealth} HP";
		if (c.Poison > 0) text += $" ({c.Poison} poison)";
		return text;
	}

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

	public DiceController getPlayerDice()
	{
		return playerDice;
	}

	public DiceController getEnemyDice()
	{
		return enemyDice;
	}

	public DiceTokenLayer getDiceTokenLayer()
	{
		return tokenLayer;
	}
}
