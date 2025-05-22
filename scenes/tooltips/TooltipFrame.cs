using Godot;
using System;

public partial class TooltipFrame : Control
{
    MarginContainer outerMarginContainer = null!;

    public override void _Ready()
    {
        outerMarginContainer = GetNode<MarginContainer>("%OuterMarginContainer");

        ((GodotObject)GD.Load("res://servers/game_state_server.gd").Get("global_signals"))
            .Connect("current_selection_changed", Callable.From<Node2D>(CurrentSelectionChanged));
    }

    const float fadeDuration = 0.2f;
    void HideTooltip()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), fadeDuration);
        tween.Finished += () => Visible = false;
    }

    void ShowTooltip()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), fadeDuration);
        Visible = true;
    }

    static readonly PackedScene tooltipContentsPawnPackedScene = GD.Load<PackedScene>("res://scenes/tooltips/tooltip_contents_pawn.tscn");

    void CurrentSelectionChanged(Node2D? newSelection)
    {
        if (newSelection is null)
            HideTooltip();
        else if (newSelection is Pawn pawn)
        {
            outerMarginContainer.ClearChildren();
            
            var contents = tooltipContentsPawnPackedScene.Instantiate<TooltipContentsPawn>();
            contents.Pawn = pawn;
            outerMarginContainer.AddChild(contents);
        }
        else
            throw new NotImplementedException($"Tooltip CurrentSelectionChanged: {newSelection?.GetType()}");
    }
}
