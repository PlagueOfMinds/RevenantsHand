using Godot;

public partial class DiceToken : Control
{
    public Dice Die { get; private set; }
    public int Value => Die.getRoll();
    public Card CurrentCard {get; private set;}
    public bool Interactable {get; set; } = true;

    private bool dragging;
    private Vector2 dragOffset;
    private Card hoveredCard;
    private Card originCard;    // Card picked up from
    private Vector2 originPosition;     // where the die was picked up

    // Call after the token has been added to the tree
    public void Setup(Dice die, Basis cameraBasis, float worldSize, float pixelsPerUnit)
    {
        Die = die;
        int px = Mathf.CeilToInt(worldSize * pixelsPerUnit);
        Size = new Vector2(px, px);
        MouseFilter = MouseFilterEnum.Stop;

        var container = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(container);
        container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var viewport = new SubViewport { OwnWorld3D = true, TransparentBg = true, Size = new Vector2I(px, px) };
        container.AddChild(viewport);

        var camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = worldSize,
        };
        viewport.AddChild(camera);
        camera.GlobalBasis = cameraBasis;
        camera.GlobalPosition = cameraBasis.Z * 10f;
        camera.Current = true;

        var light = new DirectionalLight3D { RotationDegrees = new Vector3(-50f, -30f, 0f) };
        viewport.AddChild(light);

        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White,
                AmbientLightEnergy = 0.4f,
            }
        });

        die.Freeze = true;
        die.Reparent(viewport, true);
        die.GlobalPosition = Vector3.Zero;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Interactable) return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                PickUp();
            }
            else if (dragging)
            {
                Drop();
            }
            AcceptEvent();
        }
        else if (dragging && @event is InputEventMouseMotion)
        {
            GlobalPosition = GetGlobalMousePosition() + dragOffset;
            UpdateHover();
            AcceptEvent();
        }
    }

    // Called by a Card: when dice is placed
    public void AttachTo(Card card)
    {
        CurrentCard = card;
        dragging = false;
        SnapToCard();
    }

    public void PickUp()
    {
        originCard = CurrentCard;
        originPosition = GlobalPosition;

        if (CurrentCard != null)
        {
            CurrentCard.ReleaseDie();
            CurrentCard = null;
            Scale = Vector2.One;
            GlobalPosition = GetGlobalMousePosition() - Size / 2f;
        }

        dragging = true;
        dragOffset = GlobalPosition - GetGlobalMousePosition();
        MoveToFront();
    }

    private void Drop()
    {
        dragging = false;
        hoveredCard?.SetHover(false);
        hoveredCard = null;

        Card target = FindCardUnderMouse();
        if (target == null) return;


        if (target.IsOccupied)
        {
            DiceToken other = target.PlacedDie;
            target.ReleaseDie();

            if (originCard != null && !originCard.IsOccupied) originCard.PlaceDie(other);
            else other.MoveFree(originPosition);
        }

        target.PlaceDie(this);
    }  

    private void MoveFree(Vector2 globalPosition)
    {
        CurrentCard = null;
        Visible = true;
        Scale = Vector2.One;
        GlobalPosition = globalPosition;
    }

    private void SnapToCard()
    {
        if (!IsInstanceValid(CurrentCard))
        {
            CurrentCard = null;
            Visible = true;
            return;
        }

        Visible = CurrentCard.IsVisibleInTree(); // hidden while the enemy cards are shown

        Rect2 slot = CurrentCard.DieSlot.GetGlobalRect();
        float fit = Mathf.Min(1f, Mathf.Min(slot.Size.X / Size.X, slot.Size.Y / Size.Y));
        Scale = new Vector2(fit, fit);
        GlobalPosition = slot.GetCenter() - Size * fit / 2f;
    }

    private void UpdateHover()
    {
        Card card = FindCardUnderMouse();
        if (card == hoveredCard) return;

        hoveredCard?.SetHover(false);
        hoveredCard = card;
        hoveredCard?.SetHover(true);
    }

    private Card FindCardUnderMouse()
    {
        Vector2 mouse = GetGlobalMousePosition();
        foreach (Node node in GetTree().GetNodesInGroup(Card.GroupName))
        {
            if (node is Card card && card.IsVisibleInTree() && card.CanAccept(this) &&
                card.GetGlobalRect().HasPoint(mouse))
                return card;
        }
        return null;
    }
}