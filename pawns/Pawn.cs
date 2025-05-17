using Godot;

[GlobalClass]
public partial class Pawn : Node2D
{
    public string? PawnName { get; set; }
    public string? PawnNickName { get; set; }
    public string? PawnSurname { get; set; }
    public bool IsMale { get; set; }

    public string? PawnFullDisplayName => $"{PawnName}{(string.IsNullOrWhiteSpace(PawnNickName) ? null : $"\"{PawnName}\"")}{PawnSurname}";
    public string? PawnShortDisplayName => string.IsNullOrWhiteSpace(PawnNickName) ? PawnName : PawnNickName;

    Node2D? overlayRoot;
    Label nameLabel = null!;

    public Node2D? Body
    {
        get => overlayRoot?.GetChildCount() > 0 ? overlayRoot?.GetChild<Node2D>(0) : null;
        set
        {
            var oldValue = Body;
            if (value != oldValue)
            {
                // save and remove the current hat and coat
                var (eyes, hat, coat) = (Eyes, Hat, Coat);
                oldValue?.Set("hat", default);
                oldValue?.Set("coat", default);
                oldValue?.Set("eyes", default);

                while (overlayRoot?.GetChildCount() > 0)
                    overlayRoot?.RemoveChild(overlayRoot?.GetChild(0));
                overlayRoot?.AddChild(value);

                value?.Set("eyes", eyes);
                value?.Set("hat", hat);
                value?.Set("coat", coat);
            }
        }
    }

    public Node2D? Eyes
    {
        get => Body?.Get("eyes").As<Node2D?>();
        set => Body?.Set("eyes", value);
    }

    public Node2D? Hat
    {
        get => Body?.Get("hat").As<Node2D?>();
        set => Body?.Set("hat", value);
    }

    public Node2D? Coat
    {
        get => Body?.Get("coat").As<Node2D?>();
        set => Body?.Set("coat", value);
    }

    public override void _Ready()
    {
        overlayRoot = GetNode<Node2D>("%OverlayRoot");
        nameLabel = GetNode<Label>("%Name");
    }

    Vector2? lastRealPosition;
    Vector2? targetPosition;
    public override void _Process(double delta)
    {
        nameLabel.Text = PawnShortDisplayName;
        if (GetTree().GetFirstNodeInGroup("game_camera") is Camera2D { } camera)
            nameLabel.Set("theme_override_font_sizes/font_size", 45 / camera.Zoom.X);

        lastRealPosition ??= Position;

        // idle AI
        targetPosition ??= TerrainServer.Instance.GetReachablePositionInRange(lastRealPosition.Value / MapGenerationServer.Instance.TileSize, 5)
            * MapGenerationServer.Instance.TileSize;

        if ((targetPosition.Value - Position).LengthSquared() < 2f)
        {
            Position = targetPosition.Value;
            targetPosition = null;
            return;
        }

        var direction = (targetPosition.Value - Position).Normalized();
        Position += direction * (float)delta * 50f;
    }
}
