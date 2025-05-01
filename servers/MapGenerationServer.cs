using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

[GlobalClass]
public partial class MapGenerationServer : GodotObject
{
    readonly record struct Plant(string Name, Texture2D SpriteTexture);

    readonly record struct PlantSpawnChance(Plant Plant, float ChancePercentage);
    readonly record struct Biome(string Name, bool Passable, bool SpawnMines,
        double Height, double Moisture, double Heat, Texture2D[] Textures,
        PlantSpawnChance[] PlantSpawnChances)
    {
        public bool Matches(double height, double moisture, double heat) =>
            height >= Height && moisture >= Moisture && heat >= Heat;

        public double GetDifferenceFromPoint(double height, double moisture, double heat) =>
            (height - Height) + (moisture - Moisture) + (heat - Heat);
    }

    static readonly Biome[] biomes;
    static readonly Plant[] plants;
    static readonly Dictionary<Texture2D, Vector2I> groundTileIds = [], plantTileIds = [];
    static int groundTileSourceId, plantTileSourceId;
    static readonly Vector2I tileSize = new(32, 32);
    static MapGenerationServer()
    {
        // plants
        const string plantPath = "res://plants/";
        var plantFiles = DirAccess.GetFilesAt(plantPath)
            .Select(file => file.Replace(".remap", ""))
            .Where(file => file.EndsWith(".tres"))
            .ToArray();
        plants = new Plant[plantFiles.Length];

        int plantIndex = 0;
        foreach (var plantFile in plantFiles)
        {
            var plant = GD.Load<Resource>(plantPath + plantFile);
            plants[plantIndex++] = new Plant(
                plant.Get("name").AsString(),
                plant.Get("sprite_texture").As<Texture2D>());
        }
        GD.Print($"Loaded {plantFiles.Length} plants");

        // biomes
        const string biomePath = "res://biomes/";
        var biomeFiles = DirAccess.GetFilesAt(biomePath)
            .Select(file => file.Replace(".remap", ""))
            .Where(file => file.EndsWith(".tres"))
            .ToArray();
        biomes = new Biome[biomeFiles.Length];

        int biomeIndex = 0;
        foreach (var biomeFile in biomeFiles)
        {
            var biome = GD.Load<Resource>(biomePath + biomeFile);

            var tilesetPath = biomePath + "tilesets/"
                + (string.IsNullOrWhiteSpace(biome.Get("tileset").AsString()) ? biome.Get("name").AsString() : biome.Get("tileset").AsString())
                    .ToLowerInvariant().Replace(' ', '_')
                + "/";
            var tilesetTextures = DirAccess.GetFilesAt(tilesetPath)
                .Select(textureFile => textureFile.Replace(".remap", ""))
                .Where(textureFile => textureFile.EndsWith(".png"))
                .Select(textureFile => GD.Load<Texture2D>(tilesetPath + textureFile))
                .ToArray();

            var plantSpawnChances = biome.Get("plants").AsGodotArray()
                .Select(plantSpawnChance => plantSpawnChance.As<Resource>())
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
                biome.Get("spawn_mines").AsBool(),
                biome.Get("min_height").AsSingle(),
                biome.Get("min_moisture").AsSingle(),
                biome.Get("min_heat").AsSingle(),
                tilesetTextures, plantSpawnChances);
        }
        GD.Print($"Loaded {biomeFiles.Length} biomes");
    }

    public Vector2I TileSize => tileSize;

    public void InitializeGroundTileSet(TileMapLayer groundLayer)
    {
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif

        // create one image with all the tile textures
        var atlas = new TileSetAtlasSource
        {
            TextureRegionSize = tileSize,
            Margins = default,
            Separation = new(1, 1),
            UseTexturePadding = false,
        };
        var image = Image.CreateEmpty(512, 512, true, Image.Format.Rgb8);   // TODO: figure out the size from the textures
        int x = 0, y = 0, tileX = 0, tileY = 0;
        List<Vector2I> tilePositions = [];
        foreach (var texture in biomes.SelectMany(b => b.Textures))
        {
            BlitRectHelper(image, texture.GetImage(), new(0, 0, texture.GetWidth(), texture.GetHeight()), new(x, y));

            tilePositions.Add(groundTileIds[texture] = new(tileX, tileY));

            if (x + 2 * (tileSize.X + 1) >= image.GetWidth())
            {
                x = 0; tileX = 0;
                y += tileSize.Y + 1; ++tileY;
            }
            else
            {
                x += tileSize.X + 1; ++tileX;
            }
        }

        atlas.Texture = ImageTexture.CreateFromImage(image);
        foreach (var tilePosition in tilePositions)
            atlas.CreateTile(tilePosition);

        groundLayer.TileSet = new TileSet() { UVClipping = true };
        groundLayer.TileSet.TileSize = tileSize;
        groundLayer.TileSet.AddSource(atlas);
        groundTileSourceId = groundLayer.TileSet.GetSourceId(0);

#if DEBUG
        GD.Print($"Map tileset generation complete in {stopwatch.Elapsed.TotalSeconds}s");
#endif
    }

    public void InitializePlantTileSet(TileMapLayer plantLayer)
    {
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif

        // create one image with all the tile textures
        var atlas = new TileSetAtlasSource
        {
            TextureRegionSize = tileSize,
            Margins = default,
            Separation = new(1, 1),
            UseTexturePadding = false,
        };
        var image = Image.CreateEmpty(512, 512, true, Image.Format.Rgba8);   // TODO: figure out the size from the textures
        int x = 0, y = 0, tileX = 0, tileY = 0;
        List<Vector2I> tilePositions = [];
        foreach (var texture in plants.Select(b => b.SpriteTexture))
        {
            BlitRectHelper(image, texture.GetImage(), new(0, 0, texture.GetWidth(), texture.GetHeight()), new(x, y));

            tilePositions.Add(plantTileIds[texture] = new(tileX, tileY));

            if (x + 2 * (tileSize.X + 1) >= image.GetWidth())
            {
                x = 0; tileX = 0;
                y += tileSize.Y + 1; ++tileY;
            }
            else
            {
                x += tileSize.X + 1; ++tileX;
            }
        }

        atlas.Texture = ImageTexture.CreateFromImage(image);
        foreach (var tilePosition in tilePositions)
            atlas.CreateTile(tilePosition);

        plantLayer.TileSet = new TileSet() { UVClipping = true };
        plantLayer.TileSet.TileSize = tileSize;
        plantLayer.TileSet.AddSource(atlas);
        plantTileSourceId = plantLayer.TileSet.GetSourceId(0);

#if DEBUG
        GD.Print($"Map tileset generation complete in {stopwatch.Elapsed.TotalSeconds}s");
#endif
    }

    static void BlitRectHelper(Image destinationImage, Image sourceImage, Rect2I sourceRect, Vector2I destinationPosition)
    {
        if (sourceImage.GetFormat() != destinationImage.GetFormat())
        {
            // convert the image to the destination format
            sourceImage.Convert(destinationImage.GetFormat());
        }

        if(sourceRect.Size != tileSize)
        {
            // crop?
            if(sourceRect.Position != default || sourceRect.Size!=sourceImage.GetSize())
            {
                sourceImage.BlitRect(sourceImage, new(0,0, sourceImage.GetWidth(), sourceImage.GetHeight()), default);
                sourceImage.Crop(sourceImage.GetWidth(), sourceImage.GetHeight());
            }

            // resize the image
            sourceImage.Resize(tileSize.X, tileSize.Y);
        }

        destinationImage.BlitRect(sourceImage, sourceRect, destinationPosition);
    }

    record struct Wave(FastNoiseLite Noise, float Amplitude);
    public Vector2I GenerateMap(TileMapLayer groundLayer, TileMapLayer plantLayer, TileMapLayer miningLayer,
        int width, int height, Resource[] _heightWaves, Resource[] _moistureWaves, Resource[] _heatWaves)
    {
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
        try
        {
#endif

            groundLayer.Clear();
            plantLayer.Clear();
            miningLayer.Clear();

            var heightWaves = toInternalWaves(_heightWaves);
            var moistureWaves = toInternalWaves(_moistureWaves);
            var heatWaves = toInternalWaves(_heatWaves);

        rebuild:
            var cells = new Biome[width, height];
            var miningCells = new Godot.Collections.Array<Vector2I>();

            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    var heightValue = GenerateNoiseValue(x, y, heightWaves);
                    var moistureValue = GenerateNoiseValue(x, y, moistureWaves);
                    var heatValue = GenerateNoiseValue(x, y, heatWaves);

                    var bestBiomeIndex = 0;
                    var bestBiomeError = double.MaxValue;
                    for (var biomeIndex = 0; biomeIndex < biomes.Length; ++biomeIndex)
                        if (biomes[biomeIndex].Matches(heightValue, moistureValue, heatValue)
                            && biomes[biomeIndex].GetDifferenceFromPoint(heightValue, moistureValue, heatValue) is { } error
                            && error < bestBiomeError)
                        {
                            (bestBiomeIndex, bestBiomeError) = (biomeIndex, error);
                        }

                    var cellTexture = biomes[bestBiomeIndex].Textures[GD.Randi() % biomes[bestBiomeIndex].Textures.Length];
                    groundLayer.SetCell(new(x, y), groundTileSourceId, groundTileIds[cellTexture]);
                    var biome = cells[x, y] = biomes[bestBiomeIndex];

                    // spawn plants
                    if (biome.PlantSpawnChances.Length > 0)
                    {
                        var plantSpawnChance = GD.Randf();
                        foreach (var plantSpawn in biome.PlantSpawnChances)
                        {
                            if (plantSpawnChance <= plantSpawn.ChancePercentage)
                            {
                                var plantTexture = plantSpawn.Plant.SpriteTexture;
                                var plantTileId = plantTileIds[plantTexture];
                                plantLayer.SetCell(new(x, y), plantTileSourceId, plantTileId);
                                break;
                            }
                            else
                                plantSpawnChance -= plantSpawn.ChancePercentage;
                        }
                    }

                    // retain the mining resources to spawn when we know them all
                    if(biome.SpawnMines)
                    {
                        miningCells.Add(new(x, y));
                    }
                }

            // spawn mining resources
            miningLayer.SetCellsTerrainConnect(miningCells, 0, 0);

            // and center the layers
            groundLayer.Position = plantLayer.Position = miningLayer.Position =
                -new Vector2(width, height) / 2 * tileSize;

            if (FindLargestStartingLocation() is { } startingLocation)
            {
                GD.Print($"Found starting location at: {startingLocation}");
                return startingLocation;
            }

            GD.Print("No suitable starting location found, regenerating map...");
            goto rebuild;

            static Wave[] toInternalWaves(Resource[] resourceWaves) =>
                [.. resourceWaves.Select(w => new Wave(
                new()
                {
                    NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
                    Seed = w.Get("wave_seed").AsInt32(),
                    Frequency = w.Get("frequency").AsSingle(),
                    FractalType = FastNoiseLite.FractalTypeEnum.PingPong,
                },
                w.Get("amplitude").AsSingle()))];

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
                var visited = new bool[width, height];
                List<Vector2I>? largestRegionCells = default;

                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        if (visited[x, y] || !cells[x, y].Passable)
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
                                if (neighbor.X < 0 || neighbor.X >= width
                                    || neighbor.Y < 0 || neighbor.Y >= height
                                    || visited[neighbor.X, neighbor.Y]
                                    || !cells[neighbor.X, neighbor.Y].Passable)
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

#if DEBUG
        }
        finally
        {
            GD.Print($"Map generation complete in {stopwatch.Elapsed.TotalSeconds}s");
        }
#endif
    }
}
