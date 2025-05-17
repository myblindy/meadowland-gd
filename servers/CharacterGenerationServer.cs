using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;

[GlobalClass]
public partial class CharacterGenerationServer : GodotObject
{
    readonly List<string> surnames, maleNames, femaleNames, nicknames;

    public static CharacterGenerationServer Instance { get; private set; } = null!;
    public CharacterGenerationServer()
    {
        Debug.Assert(Instance is null);
        Instance = this;

        surnames = [.. GetLineStrings("res://names/surnames.txt")];
        maleNames = [.. GetLineStrings("res://names/names_male.txt")];
        femaleNames = [.. GetLineStrings("res://names/names_female.txt")];
        nicknames = [.. GetLineStrings("res://names/nicknames.txt")];
    }

    static IEnumerable<string> GetLineStrings(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        while (file.GetPosition() < file.GetLength())
            if (file.GetLine() is { } line)
                yield return line;
    }

    public void GenerateRandomPawn(Pawn pawn)
    {
        pawn.IsMale = GD.Randi() % 2 == 0;

        pawn.PawnName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase((pawn.IsMale ? maleNames : femaleNames).PickRandom());
        pawn.PawnNickName = GD.Randf() < 0.3f ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nicknames.PickRandom()) : null;
        pawn.PawnSurname = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(surnames.PickRandom());

        pawn.Body = bodies[GD.Randi() % bodies.Length].Instantiate<Node2D>();

        // skin
        pawn.Body.Set("skin", new Color(GD.Randf(), GD.Randf(), GD.Randf()));

        // eyes
        var eyeScene = eyes[GD.Randi() % eyes.Length];
        if (eyeScene is not null)
        {
            var eyes = eyeScene.Instantiate<Node2D>();
            eyes.Set("color", new Color(GD.Randf(), GD.Randf(), GD.Randf()));
            pawn.Eyes = eyes;
        }

        // hat
        var coatScene = coats[GD.Randi() % coats.Length];
        if (coatScene is not null)
        {
            var coat = coatScene.Instantiate<Node2D>();
            coat.Set("color", new Color(GD.Randf(), GD.Randf(), GD.Randf()));
            pawn.Coat = coat;
        }

        // hat
        var hatScene = hats[GD.Randi() % hats.Length];
        if (hatScene is not null)
        {
            var hat = hatScene.Instantiate<Node2D>();
            hat.Set("color", new Color(GD.Randf(), GD.Randf(), GD.Randf()));
            pawn.Hat = hat;
        }
    }

    static readonly PackedScene[] eyes = [
        GD.Load<PackedScene>("res://pawns/parts/eyes/eyes01.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/eyes/eyes02.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/eyes/eyes03.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/eyes/eyes04.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/eyes/eyes05.tscn"),
        ];

    static readonly PackedScene[] bodies = [
        GD.Load<PackedScene>("res://pawns/parts/body/slim_body.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/body/fat_body.tscn"),
        ];

    static readonly PackedScene?[] coats = [
        null,
        GD.Load<PackedScene>("res://pawns/parts/coats/pauldrons_coat.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/coats/jacket_coat.tscn"),
        ];

    static readonly PackedScene?[] hats = [
        null,
        GD.Load<PackedScene>("res://pawns/parts/hat/beanie_hat.tscn"),
        GD.Load<PackedScene>("res://pawns/parts/hat/warm_hat.tscn"),
        ];
}