using Godot;

[GlobalClass]
public partial class Pawn : Node2D
{
    public string? PawnName { get; set; }
    public bool IsMale { get; set; }

    public Node2D? Body
    {
        get => GetChildCount() > 0 ? GetChild<Node2D>(0) : null;
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

                while (GetChildCount() > 0)
                    RemoveChild(GetChild(0));
                AddChild(value);

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

    Vector2? lastRealPosition;
    Vector2? targetPosition;
    public override void _Process(double delta)
    {
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
