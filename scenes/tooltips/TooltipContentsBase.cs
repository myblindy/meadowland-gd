using Godot;
using System;

public partial class TooltipContentsBase : Control
{
    protected Label nameLabel = null!, subtitleLabel = null!;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("%NameLabel");
        subtitleLabel = GetNode<Label>("%SubtitleLabel");
    }

    protected void SetSubtitle(string? subtitle, bool quoted=false)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
            subtitleLabel.Visible = false;
        else
        {
            subtitleLabel.Text = $"\"{subtitle}\"";
            subtitleLabel.Visible = true;
        }
    }
}
