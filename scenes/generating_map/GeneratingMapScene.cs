using Godot;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class GeneratingMapScene : Node2D
{
    public Vector2I MapSize { get; set; }

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

            // generate the pawns around the starting location
            for (int i = 0; i < 5; ++i)
                CreateRandomPawnAtPosition(TerrainServer.Instance.GetReachablePositionInRange(startingLocation, 5));

            static Node2D CreateRandomPawnAtPosition(Vector2I pawnPosition)
            {
                var body = bodies[GD.Randi() % bodies.Length].Instantiate<Node2D>();

                // skin
                body.Set("skin", new Color(GD.Randf(), GD.Randf(), GD.Randf()));

                // hat
                var coatScene = coats[GD.Randi() % coats.Length];
                if (coatScene is not null)
                {
                    var coat = coatScene.Instantiate<Node2D>();
                    coat.Set("color", new Color(GD.Randf(), GD.Randf(), GD.Randf()));
                    body.Set("coat", coat);
                }
                else
                    body.Set("coat", default);

                // hat
                var hatScene = hats[GD.Randi() % hats.Length];
                if (hatScene is not null)
                {
                    var hat = hatScene.Instantiate<Node2D>();
                    hat.Set("color", new Color(GD.Randf(), GD.Randf(), GD.Randf()));
                    body.Set("hat", hat);
                }
                else
                    body.Set("hat", default);

                return body;
            }
        });

        // navigate to the main scene
        GetTree().Root.AddChild(mainScene);
        QueueFree();
    }

    static readonly PackedScene[] bodies = [
        GD.Load<PackedScene>("res://pawns/parts/body/slim_body.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/body/fat_body.tscn"),
        ];

    static readonly PackedScene?[] coats = [
        null,
        GD.Load<PackedScene>("res://pawns/parts/coats/pauldrons_coat.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/coats/jacket_coat.tscn"),
        ];

    static readonly PackedScene?[] hats = [
        null,
        GD.Load<PackedScene>("res://pawns/parts/hat/beanie_hat.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/hat/warm_hat.tscn"),
        ];
}
