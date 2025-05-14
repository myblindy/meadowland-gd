using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

[GlobalClass]
public partial class MapGenerationServer : GodotObject
{
    readonly record struct Plant(string Name, Image[] SpriteImages);

    readonly record struct PlantSpawnChance(Plant Plant, float ChancePercentage);
    readonly record struct Biome(string Name, bool IsPassable, bool IsWater, bool SpawnMines,
        double Height, double Moisture, double Heat, Image[] Images,
        PlantSpawnChance[] PlantSpawnChances)
    {
        public bool Matches(double height, double moisture, double heat) =>
            height >= Height && moisture >= Moisture && heat >= Heat;

        public double GetDifferenceFromPoint(double height, double moisture, double heat) =>
            height - Height + moisture - Moisture + heat - Heat;
    }

    readonly Biome[] biomes;
    readonly Plant[] plants;
    readonly Dictionary<(Image image, int index), Vector2I> tileIds = [];
    int groundTileSourceId, plantTileSourceId;
    readonly Vector2I tileSize = new(32, 32);

    public static MapGenerationServer Instance { get; private set; } = null!;

    public MapGenerationServer()
    {
        Debug.Assert(Instance is null);
        Instance = this;

        // plants
        using (DurationLogger.LogDuration("Loading plants"))
        {
            const string plantPath = "res://plants/";
            var plantFiles = AssetLoadingHelpers.EnumerateAssets(plantPath, ".tres").ToArray();
            plants = new Plant[plantFiles.Length];

            int plantIndex = 0;
            foreach (var plantFile in plantFiles)
            {
                var plant = GD.Load<Resource>(plantPath + plantFile);
                plants[plantIndex++] = new Plant(
                    plant.Get("name").AsString(),
                    plant.Get("sprite_textures").AsGodotObjectArray<Image>());
            }
            GD.Print($"Loaded {plantFiles.Length} plants with {plants.Sum(p => p.SpriteImages.Length)} textures");
        }

        // biomes
        using (DurationLogger.LogDuration("Loading biomes"))
        {
            const string biomePath = "res://biomes/";
            var biomeFiles = AssetLoadingHelpers.EnumerateAssets(biomePath, ".tres").ToArray();
            biomes = new Biome[biomeFiles.Length];

            int biomeIndex = 0;
            foreach (var biomeFile in biomeFiles)
            {
                var biome = GD.Load<Resource>(biomePath + biomeFile);

                var tilesetImages = biome.Get("tileset").AsGodotObjectArray<Image>();

                var plantSpawnChances = biome.Get("plants").AsGodotObjectArray<Resource>()
                    .Select(plantSpawnChance =>
                    {
                        var plantName = plantSpawnChance.Get("plant").As<Resource>().Get("name").AsString();
                        return new PlantSpawnChance(
                            plants.Single(p => p.Name == plantName),
                            plantSpawnChance.Get("chance_percentage").AsSingle());
                    })
                    .ToArray();
                if (plantSpawnChances.Sum(w => w.ChancePercentage) is { } totalChancePercentage && totalChancePercentage > 1)
                {
                    // normalize the chances
                    plantSpawnChances = [.. plantSpawnChances.Select(w =>
                    new PlantSpawnChance(w.Plant, w.ChancePercentage / totalChancePercentage))];
                }

                biomes[biomeIndex++] = new Biome(
                    biome.Get("name").AsString(),
                    biome.Get("movement_modifier").AsSingle() > 0,
                    biome.Get("is_water").AsBool(),
                    biome.Get("spawn_mines").AsBool(),
                    biome.Get("min_height").AsSingle(),
                    biome.Get("min_moisture").AsSingle(),
                    biome.Get("min_heat").AsSingle(),
                    tilesetImages, plantSpawnChances);
            }
            GD.Print($"Loaded {biomeFiles.Length} biomes with {biomes.Sum(b => b.Images.Length)} textures");
        }
    }

    public Vector2I TileSize => tileSize;

    public void InitializeGroundTileSet(TileMapLayer groundLayer)
    {
        using (DurationLogger.LogDuration("Ground tileset generation"))
        {
            // create one image with all the tile textures
            var atlas = new TileSetAtlasSource
            {
                TextureRegionSize = tileSize,
                Margins = default,
                Separation = new(1, 1),
                UseTexturePadding = false,
            };

            List<Vector2I> tilePositions = [];
            AssetLoadingHelpers.PackTileImagesIntoAtlasImage(
                [.. biomes.SelectMany(p => p.Images)],
                tileSize, null, Image.Format.Rgb8, out var atlasImage,
                (sourceTileImage, tileImage, index, tileX, tileY) => { lock (tileIds) tilePositions.Add(tileIds[(sourceTileImage, index)] = new(tileX, tileY)); });

            atlas.Texture = ImageTexture.CreateFromImage(atlasImage);
            foreach (var tilePosition in tilePositions)
                atlas.CreateTile(tilePosition);

            groundLayer.TileSet = new TileSet() { UVClipping = true, TileSize = tileSize };
            groundLayer.TileSet.AddSource(atlas);
            groundTileSourceId = groundLayer.TileSet.GetSourceId(0);
        }
    }

    public void InitializePlantTileSet(TileMapLayer plantLayer, TileMapLayer plantLayer2)
    {
        using (DurationLogger.LogDuration("Plant tileset generation"))
        {
            Vector2I internalTileSize = new(64, 64);

            // create one image with all the tile textures
            Vector2I separation = new(50, 1);   // separation for the swaying shader
            var atlas = new TileSetAtlasSource
            {
                TextureRegionSize = internalTileSize,
                Margins = default,
                Separation = separation,
                UseTexturePadding = false,
            };

            List<Vector2I> tilePositions = [];
            AssetLoadingHelpers.PackTileImagesIntoAtlasImage(
                [.. plants.SelectMany(p => p.SpriteImages)],
                internalTileSize, null, Image.Format.Rgba8, out var image,
                (sourceTileImage, tileImage, index, tileX, tileY) => { lock (tileIds) tilePositions.Add(tileIds[(sourceTileImage, index)] = new(tileX, tileY)); },
                separation);

            atlas.Texture = ImageTexture.CreateFromImage(image);
            foreach (var tilePosition in tilePositions)
                atlas.CreateTile(tilePosition);

            plantLayer.TileSet = new TileSet() { UVClipping = true, TileSize = internalTileSize };
            plantLayer.TileSet.AddSource(atlas);
            plantTileSourceId = plantLayer.TileSet.GetSourceId(0);

            plantLayer2.TileSet = plantLayer.TileSet;
        }
    }

    SimpleAutoTilerHelper? simpleAutoTilerHelper;
    public void InitializeMiningTileSet(TileMapLayer miningLayer)
    {
        using (DurationLogger.LogDuration("Learning mining tileset"))
            simpleAutoTilerHelper = new(miningLayer.TileSet);
    }

    record struct Wave(FastNoiseLite Noise, float Amplitude);
    public Vector2I GenerateMap(Node mainScene, Vector2I mapSize,
        Vector3[] _heightWaves, Vector3[] _moistureWaves, Vector3[] _heatWaves)
    {
        using (DurationLogger.LogDuration("Map generation"))
        {
            var groundLayer = mainScene.GetNode<TileMapLayer>("TileMapGroundLayer");
            var plantLayer = mainScene.GetNode<TileMapLayer>("TileMapPlantLayer");
            var plantLayer2 = mainScene.GetNode<TileMapLayer>("TileMapPlantLayer2");
            var miningLayer = mainScene.GetNode<TileMapLayer>("TileMapMiningLayer");

            var gameResourcesServer = GameResourcesServer.Instance;
            var terrainServer = TerrainServer.Instance;
            terrainServer.Initialize(mapSize.X, mapSize.Y);

            groundLayer.Clear();
            plantLayer.Clear();
            miningLayer.Clear();
            plantLayer2.Clear();

            var heightWaves = toInternalWaves(_heightWaves);
            var moistureWaves = toInternalWaves(_moistureWaves);
            var heatWaves = toInternalWaves(_heatWaves);

            (float height, float moisture, float heat)[] noiseValues = new (float, float, float)[mapSize.X * mapSize.Y];
            Partitioner.Create(0, mapSize.X * mapSize.Y)
                .AsParallel()
                .ForAll(partition =>
                {
                    for (int i = partition.Item1; i < partition.Item2; ++i)
                    {
                        var (x, y) = (i % mapSize.X, i / mapSize.X);

                        noiseValues[i] = (
                            GenerateNoiseValue(x, y, heightWaves),
                            GenerateNoiseValue(x, y, moistureWaves),
                            GenerateNoiseValue(x, y, heatWaves));
                    }
                });

rebuild:
            var cells = new Biome[mapSize.X, mapSize.Y];
            var miningCells = new List<(Vector2I Position, int SourceId)>();

            using (DurationLogger.LogDuration("Generating map biomes and setting ground and plant tilemaps"))
            {
                for (int y = 0; y < mapSize.Y; ++y)
                    for (int x = 0; x < mapSize.X; ++x)
                    {
                        var (heightValue, moistureValue, heatValue) = noiseValues[x + y * mapSize.X];

                        var bestBiomeIndex = 0;
                        var bestBiomeError = double.MaxValue;
                        for (var biomeIndex = 0; biomeIndex < biomes.Length; ++biomeIndex)
                            if (biomes[biomeIndex].Matches(heightValue, moistureValue, heatValue)
                                && biomes[biomeIndex].GetDifferenceFromPoint(heightValue, moistureValue, heatValue) is { } error
                                && error < bestBiomeError)
                            {
                                (bestBiomeIndex, bestBiomeError) = (biomeIndex, error);
                            }

                        var cellTexture = biomes[bestBiomeIndex].Images[GD.Randi() % biomes[bestBiomeIndex].Images.Length];
                        groundLayer.SetCell(new(x, y), groundTileSourceId, tileIds[(cellTexture, 0)]);
                        var biome = cells[x, y] = biomes[bestBiomeIndex];
                        terrainServer.SetWater(x, y, biome.IsWater);

                        // spawn plants
                        if (biome.PlantSpawnChances.Length > 0)
                        {
                            var plantSpawnChance = GD.Randf();
                            foreach (var plantSpawn in biome.PlantSpawnChances)
                            {
                                if (plantSpawnChance <= plantSpawn.ChancePercentage)
                                {
                                    var plantImage = plantSpawn.Plant.SpriteImages[
                                        GD.Randi() % plantSpawn.Plant.SpriteImages.Length];
                                    var plantTileId = tileIds[(plantImage, 0)];
                                    plantLayer.SetCell(new(x, y), plantTileSourceId, plantTileId);

                                    // second plant layer
                                    if (tileIds.TryGetValue((plantImage, 1), out var plantTileId2))
                                        plantLayer2.SetCell(new(x, y - 1), plantTileSourceId, plantTileId2);

                                    break;
                                }
                                else
                                    plantSpawnChance -= plantSpawn.ChancePercentage;
                            }
                        }

                        // retain the mining resources to spawn when we know them all
                        if (biome.SpawnMines)
                        {
                            // TODO: different mines
                            miningCells.Add((new(x, y), 0));
                            terrainServer.SetMine(x, y, gameResourcesServer.GetMineResource("Sandstone"));
                        }
                    }
            }

            // spawn mining resources
            using (DurationLogger.LogDuration("Setting mining resource tilemap"))
                simpleAutoTilerHelper!.SetCellsConnectedSimple(miningLayer, miningCells);

            using (DurationLogger.LogDuration("Finding the starting location"))
                if (FindLargestStartingLocation() is { } startingLocation)
                {
                    GD.Print($"Found starting location at: {startingLocation}");
                    return startingLocation;
                }

            GD.Print("No suitable starting location found, regenerating map...");
            goto rebuild;

            static Wave[] toInternalWaves(Vector3[] resourceWaves) =>
                [.. resourceWaves.Select(w => new Wave(
                new()
                {
                    NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
                    Seed = (int)w.X,
                    Frequency = w.Y,
                    FractalType = FastNoiseLite.FractalTypeEnum.PingPong,
                },
                w.Z))];

            static float GenerateNoiseValue(int x, int y, Wave[] waves)
            {
                float result = 0, normalization = 0;
                foreach (ref var wave in waves.AsSpan())
                {
                    result += wave.Amplitude * (wave.Noise.GetNoise2D(x, y) + 1) / 2;
                    normalization += wave.Amplitude;
                }

                return result / normalization;
            }

            Vector2I? FindLargestStartingLocation()
            {
                // find all connected passable cell biomes
                var visited = new bool[mapSize.X, mapSize.Y];
                List<Vector2I>? largestRegionCells = default;

                for (int y = 0; y < mapSize.Y; ++y)
                {
                    for (int x = 0; x < mapSize.X; ++x)
                    {
                        if (visited[x, y] || !cells[x, y].IsPassable || cells[x, y].SpawnMines)
                            continue;

                        var openCells = new Stack<Vector2I>();
                        var currentRegionCells = new List<Vector2I> { new(x, y) };
                        openCells.Push(new(x, y));

                        while (openCells.Count > 0)
                        {
                            var current = openCells.Pop();
                            visited[current.X, current.Y] = true;

                            foreach (var offset in (ReadOnlySpan<Vector2I>)
                                [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)])
                            {
                                var neighbor = current + offset;
                                if (neighbor.X < 0 || neighbor.X >= mapSize.X
                                    || neighbor.Y < 0 || neighbor.Y >= mapSize.Y
                                    || visited[neighbor.X, neighbor.Y]
                                    || !cells[neighbor.X, neighbor.Y].IsPassable)
                                    continue;

                                visited[neighbor.X, neighbor.Y] = true;
                                openCells.Push(neighbor);
                                currentRegionCells.Add(neighbor);
                            }
                        }

                        if (largestRegionCells is null || largestRegionCells.Count < currentRegionCells.Count)
                            largestRegionCells = currentRegionCells;
                    }
                }

                if (largestRegionCells is null)
                    return null;

                // find the center of the largest region
                var center = new Vector2I(
                    (int)largestRegionCells.Average(c => c.X),
                    (int)largestRegionCells.Average(c => c.Y));

                // find the cell closest to the center
                var closestCell = largestRegionCells
                    .MinBy(cell => (cell - center).LengthSquared());
                return closestCell;
            }
        }
    }
}
