using System;
using System.Diagnostics;
using System.Linq;
using Godot;

[GlobalClass]
public partial class MapGenerationServer : GodotObject
{
    readonly record struct Biome(string Name, double Height, double Moisture, double Heat, Texture[] Textures)
    {
        public bool Matches(double height, double moisture, double heat) =>
            height >= Height && moisture >= Moisture && heat >= Heat;

        public double GetDifferenceFromPoint(double height, double moisture, double heat) =>
            (height - Height) + (moisture - Moisture) + (Heat - heat);
    }

    static readonly Biome[] Biomes;
    static MapGenerationServer()
    {
        const string biomePath = "res://biomes/";
        var biomeFiles = DirAccess.GetFilesAt(biomePath);
        Biomes = new Biome[biomeFiles.Length];

        int biomeIndex = 0;
        foreach (var biomeFile in biomeFiles)
        {
            var biome = GD.Load<Resource>(biomePath + biomeFile);

            var tilesetPath = biomePath + "tilesets/"
                + (string.IsNullOrWhiteSpace(biome.Get("tileset").AsString()) ? biome.Get("name").AsString() : biome.Get("tileset").AsString()) 
                    .Replace(' ', '_')
                + "/";
            GD.Print($"Loading tileset from {tilesetPath} for biome {biome.Get("name").AsString()}");
            var tilesetTextures = DirAccess.GetFilesAt(tilesetPath)
                .Where(textureFile => textureFile.EndsWith(".png"))
                .Select(textureFile => GD.Load<Texture2D>(tilesetPath + textureFile))
                .ToArray();

            Biomes[biomeIndex++] = new Biome(
                biome.Get("name").AsString(),
                biome.Get("min_height").AsSingle(),
                biome.Get("min_moisture").AsSingle(),
                biome.Get("min_heat").AsSingle(),
                tilesetTextures);
        }

        GD.Print($"Loaded {biomeFiles.Length} biomes");
    }

    record struct Wave(FastNoiseLite Noise, float Amplitude);
    public void GenerateMap(int width, int height,
        Resource[] _heightWaves, Resource[] _moistureWaves, Resource[] _heatWaves)
    {
        var heightWaves = toInternalWaves(_heightWaves);
        var moistureWaves = toInternalWaves(_moistureWaves);
        var heatWaves = toInternalWaves(_heatWaves);

        var result = new (double Height, Texture texture)[width, height];

        for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                var heightValue = GenerateNoiseValue(x, y, heightWaves);
                var moistureValue = GenerateNoiseValue(x, y, moistureWaves);
                var heatValue = GenerateNoiseValue(x, y, heatWaves);

                var bestBiomeIndex = 0;
                var bestBiomeError = double.MaxValue;
                for (var biomeIndex = 0; biomeIndex < Biomes.Length; ++biomeIndex)
                    if (Biomes[biomeIndex].Matches(heightValue, moistureValue, heatValue)
                        && Biomes[biomeIndex].GetDifferenceFromPoint(heightValue, moistureValue, heatValue) is { } error
                        && error < bestBiomeError)
                    {
                        (bestBiomeIndex, bestBiomeError) = (biomeIndex, error);
                    }

                result[x, y] = (heightValue, Biomes[bestBiomeIndex].Textures[GD.Randi() % Biomes[bestBiomeIndex].Textures.Length]);
            }

        static Wave[] toInternalWaves(Resource[] resourceWaves) =>
            [.. resourceWaves.Select(w => new Wave(
                new()
                {
                    NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
                    Seed = w.Get("seed").AsInt32(),
                    Frequency = w.Get("frequency").AsSingle(),
                },
                w.Get("amplitude").AsSingle()))];

        static float GenerateNoiseValue(int x, int y, Wave[] waves)
        {
            float result = 0, normalization = 0;
            foreach (ref var wave in waves.AsSpan())
            {
                result += wave.Amplitude * wave.Noise.GetNoise2D(x, y);
                normalization += wave.Amplitude;
            }

            return result / normalization;
        }
    }
}
