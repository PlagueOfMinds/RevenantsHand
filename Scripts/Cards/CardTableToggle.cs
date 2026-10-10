using Godot;

public partial class CardTableToggle : Button
{
    [Export] private Control playerCardRow;
    [Export] private Control enemyCardRow;

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
        Text = showEnemy ? "Your Cards" : "Enemy Cards";
    }
}