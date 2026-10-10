using Godot;

public partial class CardTableToggle : Button
{
    [Export] private Control playerCardRow;
    [Export] private Control enemyCardRow;
    [Export] private Control playerHealthLabel;
    [Export] private Control enemyHealthLabel;

    public override void _Ready()
    {
        ToggleMode = true;
        Toggled += ShowEnemyCards;
        ShowEnemyCards(false);
    }

    private void ShowEnemyCards(bool showEnemy)
    {
        playerCardRow.Visible = !showEnemy;
        enemyCardRow.Visible = showEnemy;

        if (playerHealthLabel != null) playerHealthLabel.Visible = !showEnemy;
        if (enemyHealthLabel != null) enemyHealthLabel.Visible = showEnemy;

        Text = showEnemy ? "Your Cards" : "Enemy Cards";
    }
}