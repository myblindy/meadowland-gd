public partial class TooltipContentsPawn : TooltipContentsBase
{
    public Pawn? Pawn{ get; set; }

    public override void _Ready()
    {
        base._Ready();

        if (Pawn is not null)
        {
            nameLabel.Text = Pawn.PawnFullDisplayNameWithoutNickName;
            SetSubtitle(Pawn.PawnNickName, true);
        }
    }
}
