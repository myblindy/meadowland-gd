using Godot;
using System.Threading.Tasks;

public partial class GeneratingMapScene : Node2D
{
    public Vector2I MapSize { get; set; }
    public Node2D[] Characters { get; set; } = [];

    public override async void _Ready()
    {
        GD.Print($"Generating map with {MapSize.X}x{MapSize.Y} size...");

        var mainScene = GD.Load<PackedScene>("res://scenes/main/main_scene.tscn").Instantiate();

        await Task.Run(async () =>
        {
            await Task.WhenAll(
                Task.Run(() => MapGenerationServer.Instance.InitializeGroundTileSet(
                    mainScene.GetNode<TileMapLayer>("TileMapGroundLayer"))),
                Task.Run(() => MapGenerationServer.Instance.InitializePlantTileSet(
                    mainScene.GetNode<TileMapLayer>("TileMapPlantLayer"),
                    mainScene.GetNode<TileMapLayer>("TileMapPlantLayer2"))),
                Task.Run(() => MapGenerationServer.Instance.InitializeMiningTileSet(
                    mainScene.GetNode<TileMapLayer>("TileMapMiningLayer")))).ConfigureAwait(false);

            var startingLocation = MapGenerationServer.Instance.GenerateMap(mainScene, MapSize,
                [
                    new(GD.Randi(), 0.004f, 1.0f),
                    new(GD.Randi(), 0.002f, 0.5f),
                ],
                [
                    new(GD.Randi(), 0.002f, 1.0f),
                ],
                [
                    new(GD.Randi(), 0.002f, 1.0f),
                    new(GD.Randi(), 0.001f, 0.5f),
                ]);

            mainScene.GetNode<Camera2D>("Camera2D").Position = startingLocation * MapGenerationServer.Instance.TileSize;

            // add the pawns around the starting location
            foreach(var character in Characters)
            {
                character.Position = TerrainServer.Instance.GetReachablePositionInRange(startingLocation, 5)
                    * MapGenerationServer.Instance.TileSize;
                character.Scale = new Vector2(0.25f, 0.25f);
                mainScene.GetNode<Node2D>("PawnRoot").AddChild(character);
            }
        });

        // navigate to the main scene
        GetTree().Root.AddChild(mainScene);
        QueueFree();
    }
}
