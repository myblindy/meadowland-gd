using Godot;

public static class NodeExtensions
{
    public static void ClearChildren(this Node node)
    {
        while (node.GetChildCount() > 0)
            node.RemoveChild(node.GetChild(-1));
    }
}