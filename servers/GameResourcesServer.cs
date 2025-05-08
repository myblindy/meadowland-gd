using System.Collections.Generic;
using System.Diagnostics;
using Godot;

[GlobalClass]
public partial class GameResourcesServer : GodotObject
{
    public static GameResourcesServer Instance { get; private set; } = null!;
    readonly Dictionary<string, Resource> mineResources = [];
    public GameResourcesServer()
    {
        Debug.Assert(Instance is null);
        Instance = this;

        const string mineResourcesPath = "res://mines/";
        foreach (var mineResource in AssetLoadingHelpers.EnumerateAssets(mineResourcesPath, ".tres"))
        {
            var resource = GD.Load<Resource>(mineResourcesPath + mineResource);
            mineResources[resource.Get("name").AsString()] = resource;
        }
        GD.Print($"Loaded {mineResources.Count} mine resources");
    }

    public IEnumerable<Resource> EnumerateMineResources() => mineResources.Values;
    public Resource? GetMineResource(string name) => mineResources.TryGetValue(name, out var resource) ? resource : null;
}
