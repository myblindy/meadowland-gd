using System.Collections;
using System.Collections.Generic;
using Godot;

static class HelperEnumerableExtensions
{
    public static T PickRandom<T>(this IList<T> list)
    {
        var randomIndex = (int)(GD.Randi() % list.Count);
        return list[randomIndex];
    }
}