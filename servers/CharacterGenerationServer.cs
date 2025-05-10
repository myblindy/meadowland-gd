using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;

[GlobalClass]
public partial class CharacterGenerationServer : GodotObject
{
    readonly List<string> surnames, maleNames, femaleNames;

    public static CharacterGenerationServer Instance { get; private set; } = null!;
    public CharacterGenerationServer()
    {
        Debug.Assert(Instance is null);
        Instance = this;

        surnames = [.. FileAccess.GetFileAsString("res://names/surnames.txt").Split('\n')];
        maleNames = [.. FileAccess.GetFileAsString("res://names/names_male.txt").Split('\n')];
        femaleNames = [.. FileAccess.GetFileAsString("res://names/names_female.txt").Split('\n')];
    }

    public Pawn GenerateRandomPawn()
    {
        var pawn = GD.Load<PackedScene>("res://pawns/pawn.tscn").Instantiate<Pawn>();
        pawn.IsMale = GD.Randi() % 2 == 0;
        pawn.PawnName = GenerateRandomName(pawn.IsMale);

        pawn.Body = bodies[GD.Randi() % bodies.Length].Instantiate<Node2D>();

        // skin
        pawn.Body.Set("skin", new Color(GD.Randf(), GD.Randf(), GD.Randf()));

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

        return pawn;
    }

    public string GenerateRandomName(bool isMale)
    {
        var name = isMale ? maleNames[GD.RandRange(0,  maleNames.Count - 1)] : femaleNames[GD.RandRange(0, femaleNames.Count - 1)];
        var surname = surnames[GD.RandRange(0, surnames.Count - 1)];
        return $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name)} {CultureInfo.InvariantCulture.TextInfo.ToTitleCase(surname)}";
    }

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