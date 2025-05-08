using System.Diagnostics;
using AStar;
using AStar.Options;
using Godot;

[GlobalClass]
public partial class TerrainServer : GodotObject
{
    public static TerrainServer Instance { get; private set; } = null!;
    public TerrainServer()
    {
        Debug.Assert(Instance is null);
        Instance = this;
    }

    record struct Cell(Resource? MineResource, bool IsWater)
    {
        public readonly bool Passable => MineResource is null && !IsWater;
    }
    Cell[,] cells = new Cell[0, 0];
    WorldGrid aStarWorldGrid = null!;

    static readonly PathFinderOptions pathFinderOptions = new()
    {
        UseDiagonals = true,
        PunishChangeDirection = true,
    };

    public void Initialize(int width, int height)
    {
        cells = new Cell[width, height];
        aStarWorldGrid = new(width, height);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                aStarWorldGrid[x, y] = 1;
    }

    public void SetMine(int x, int y, Resource? mineResource)
    {
        cells[x, y].MineResource = mineResource;
        aStarWorldGrid[x, y] = (short)(mineResource is not null ? 0 : 1);
    }

    public void SetWater(int x, int y, bool isWater)
    {
        cells[x, y].IsWater = isWater;
        aStarWorldGrid[x, y] = (short)(isWater ? 0 : 1);
    }

    public bool IsPassable(int x, int y) => cells[x, y].Passable;

    public Vector2I GetReachablePositionInRange(Vector2I center, int range)
    {
        if (!IsPassable(center.X, center.Y))
            return Vector2I.MinValue;

        var aStar = new PathFinder(aStarWorldGrid, pathFinderOptions);

        while (true)
        {
            var destination = center + new Vector2I(GD.RandRange(-range, range), GD.RandRange(-range, range));
            if (IsPassable(destination.X, destination.Y))
                if (aStar.FindPath(new Position(center.X, center.Y), new Position(destination.X, destination.Y)) is { Length: > 0 })
                    return destination;
        }
    }
}