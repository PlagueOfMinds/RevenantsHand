using Godot;

// A free-floating number for a combatant's health or poison. Put it on any Label and place it anywhere.
public partial class StatLabel : Label
{
    public enum Stat { Health, Poison }

    [Export] private Stat stat = Stat.Health;
    [Export] private bool showMax = false;      // health only: "30" or "30/30"
    [Export] private bool hideWhenZero = false; // e.g. poison only shows while poisoned

    [ExportGroup("Colors")]
    [Export] private Color healthyColor = new Color("#FB3B02");  // health: full
    [Export] private Color hurtColor = new Color("#FB3B02");     // health: half
    [Export] private Color criticalColor = new Color("#FB3B02"); // health: near death
    [Export] private Color poisonColor = new Color("#FB3B02");   // poison

    private int lastValue = -1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; // never blocks clicks on dice or cards
        if (stat == Stat.Poison) AddThemeColorOverride("font_color", poisonColor);
        if (hideWhenZero) Visible = false;
    }

    public void Display(Combatant c)
    {
        if (c == null) return;

        int value = stat == Stat.Health ? c.Health : c.Poison;

        Text = stat == Stat.Health && showMax ? $"{c.Health}/{c.MaxHealth}" : value.ToString();

        if (stat == Stat.Health)
            AddThemeColorOverride("font_color", HealthColor((float)c.Health / c.MaxHealth));

        if (hideWhenZero) Visible = value > 0;

        if (stat == Stat.Health && lastValue >= 0 && value < lastValue) Pulse();
        lastValue = value;
    }

    // Blends healthy → hurt → critical as health drops
    private Color HealthColor(float fraction)
    {
        fraction = Mathf.Clamp(fraction, 0f, 1f);
        return fraction >= 0.5f
            ? hurtColor.Lerp(healthyColor, (fraction - 0.5f) * 2f)
            : criticalColor.Lerp(hurtColor, fraction * 2f);
    }

    // Quick scale pop when health drops
    private void Pulse()
    {
        PivotOffset = Size / 2f;
        Scale = new Vector2(1.3f, 1.3f);
        CreateTween().TweenProperty(this, "scale", Vector2.One, 0.25f)
                     .SetTrans(Tween.TransitionType.Back)
                     .SetEase(Tween.EaseType.Out);
    }
}