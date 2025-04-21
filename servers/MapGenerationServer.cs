using Godot;
using System;
using System.Threading.Tasks;

[Tool, GlobalClass]
public partial class MapGenerationServer : GodotObject
{
    public static async Task GenerateMapAsync(int width, int height)
    {
        await Task.Delay(1);
    }
}
