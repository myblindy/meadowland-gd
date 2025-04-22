using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

[GlobalClass]
public partial class MapGenerationServer : GodotObject
{
    readonly record struct Biome(string Name, double Height, double Moisture, double Heat, Texture2D[] Textures)
    {
        public bool Matches(double height, double moisture, double heat) =>
            height >= Height && moisture >= Moisture && heat >= Heat;

        public double GetDifferenceFromPoint(double height, double moisture, double heat) =>
            (height - Height) + (moisture - Moisture) + (Heat - heat);
    }

    static readonly Biome[] biomes;
    static readonly Dictionary<Texture2D, Vector2I> tileIds = [];
    static int tileSourceId;
    static readonly Vector2I tileSize;
    static MapGenerationServer()
    {
        const string biomePath = "res://biomes/";
        var biomeFiles = DirAccess.GetFilesAt(biomePath);
        biomes = new Biome[biomeFiles.Length];

        int biomeIndex = 0;
        foreach (var biomeFile in biomeFiles)
        {
            var biome = GD.Load<Resource>(biomePath + biomeFile);

            var tilesetPath = biomePath + "tilesets/"
                + (string.IsNullOrWhiteSpace(biome.Get("tileset").AsString()) ? biome.Get("name").AsString() : biome.Get("tileset").AsString())
                    .Replace(' ', '_')
                + "/";
            var tilesetTextures = DirAccess.GetFilesAt(tilesetPath)
                .Where(textureFile => textureFile.EndsWith(".png"))
                .Select(textureFile => GD.Load<Texture2D>(tilesetPath + textureFile))
                .ToArray();
            tileSize = new((int)tilesetTextures[0].GetSize().X, (int)tilesetTextures[0].GetSize().Y);

            biomes[biomeIndex++] = new Biome(
                biome.Get("name").AsString(),
                biome.Get("min_height").AsSingle(),
                biome.Get("min_moisture").AsSingle(),
                biome.Get("min_heat").AsSingle(),
                tilesetTextures);
        }

        GD.Print($"Loaded {biomeFiles.Length} biomes");
    }

    public void InitializeTileSet(TileMapLayer layer)
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
            image.BlitRect(texture.GetImage(), new(0, 0, texture.GetWidth(), texture.GetHeight()), new(x, y));

            tilePositions.Add(tileIds[texture] = new(tileX, tileY));

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

        layer.TileSet = new TileSet() { UVClipping = true };
        layer.TileSet.TileSize = tileSize;
        layer.TileSet.AddSource(atlas);
        tileSourceId = layer.TileSet.GetSourceId(0);

#if DEBUG
        GD.Print($"Map tileset generation complete in {stopwatch.Elapsed.TotalSeconds}s");
#endif
    }

    record struct Wave(FastNoiseLite Noise, float Amplitude);
    public void GenerateMap(TileMapLayer layer, int width, int height,
        Resource[] _heightWaves, Resource[] _moistureWaves, Resource[] _heatWaves)
    {
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif

        var heightWaves = toInternalWaves(_heightWaves);
        var moistureWaves = toInternalWaves(_moistureWaves);
        var heatWaves = toInternalWaves(_heatWaves);

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
                layer.SetCell(new(x, y), tileSourceId, tileIds[cellTexture]);
            }

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

#if DEBUG
        GD.Print($"Map generation complete in {stopwatch.Elapsed.TotalSeconds}s");
#endif
    }
}
