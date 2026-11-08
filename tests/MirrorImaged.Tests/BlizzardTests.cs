using System.Text;
using MirrorImaged.Data;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged.Tests;

/// <summary>common.j's globals and the installed game's blizzard.j, there before a map's script runs.</summary>
[Trait("Needs", "Game")]
public sealed class BlizzardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string script) => Simulation.Load(_maps.Build($"{script}\nfunction config() end\nfunction main() end\n"));

    // common.j, 3.0.1.24371
    [InstallFact]
    public void Common_j_s_constants_are_globals_with_the_games_values()
    {
        using var simulation = Load("""
            print(JASS_MAX_ARRAY_SIZE, math.type(JASS_MAX_ARRAY_SIZE), TRUE, FALSE, PLAYER_NEUTRAL_PASSIVE, PLAYER_NEUTRAL_AGGRESSIVE)
            print(tostring(PLAYER_COLOR_RED):match("^playercolor: %x+$") ~= nil, PLAYER_COLOR_RED == ConvertPlayerColor(0), PLAYER_COLOR_RED == PLAYER_COLOR_BLUE)
            print(EVENT_PLAYER_UNIT_DEATH == ConvertPlayerUnitEvent(20), MAP_LOCK_SPEED == ConvertMapFlag(16384), OSKEY_BACKSPACE == ConvertOsKeyType(8))
            print(ABILITY_IF_BUTTON_POSITION_NORMAL_X == ConvertAbilityIntegerField(FourCC('abpx')), RACE_HUMAN == ConvertRace(1), ConvertRace(0) == ConvertPlayerColor(0))
            """);

        Assert.Equal([
            "0.000 print: 32768\tinteger\ttrue\tfalse\t27\t24",
            "0.000 print: true\ttrue\tfalse",
            "0.000 print: true\ttrue\ttrue",
            "0.000 print: true\ttrue\tfalse",
        ], simulation.Log);
    }

    [InstallFact]
    public void A_convert_native_checks_its_argument_and_its_handle_is_taken_where_its_type_is_asked()
    {
        using var simulation = Load("""
            print(pcall(ConvertPlayerColor, 1.5))
            print(pcall(TimerStart, PLAYER_COLOR_RED, 1, false, nil))
            """);

        Assert.Equal([
            "0.000 print: false\tbad argument #1 to 'ConvertPlayerColor' (number has no integer representation)",
            "0.000 print: false\tbad argument #1 to 'TimerStart' (timer expected, got playercolor)",
        ], simulation.Log);
    }

    // blizzard.j, 3.0.1.24371; loading it logs nothing
    [InstallFact]
    public void Blizzard_j_s_globals_are_made_before_the_map_s_script()
    {
        using var simulation = Load("""
            print(bj_MAX_PLAYERS, bj_PLAYER_NEUTRAL_VICTIM, bj_PLAYER_NEUTRAL_EXTRA, bj_MAX_PLAYER_SLOTS, bj_PI, bj_CINEMODE_GAMESPEED == MAP_SPEED_NORMAL)
            print(tostring(bj_lastStartedTimer):match("^timer: ") ~= nil, tostring(bj_lastCreatedGroup):match("^group: ") ~= nil, bj_ELEVATOR_CODE01 == FourCC("DTrf"), bj_meleeTwinkedHeroes[3], type(bj_FORCE_PLAYER))
            """);

        Assert.Equal([
            "0.000 print: 24\t25\t26\t28\t3.14159\ttrue",
            "0.000 print: true\ttrue\ttrue\t0\ttable",
        ], simulation.Log);
    }

    [InstallFact]
    public void Every_function_blizzard_j_declares_is_a_lua_function()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var names = JassSyntaxFactory.ParseCompilationUnit(Encoding.UTF8.GetString(storage.Read("War3.w3mod:scripts/blizzard.j")!))
            .Declarations.OfType<JassFunctionDeclarationSyntax>()
            .Select(function => $"'{function.FunctionDeclarator.IdentifierName.Token.Text}'")
            .ToList();

        using var simulation = Load($$"""
            local missing = {}
            for _, name in ipairs({ {{string.Join(", ", names)}} }) do
              if type(_G[name]) ~= "function" then missing[#missing + 1] = name end
            end
            print(table.concat(missing, " "))
            """);

        Assert.True(names.Count > 1000, $"{names.Count} functions");
        Assert.Equal(["0.000 print: "], simulation.Log);
    }

    [InstallFact]
    public void A_BJ_function_runs_on_the_natives()
    {
        using var simulation = Load("""
            BJDebugMsg("to everyone")
            local t = CreateTimerBJ(false, 2)
            print(t == bj_lastStartedTimer, TimerGetRemaining(t), ModuloInteger(-7, 3))
            """);

        Assert.Equal(
            [.. Enumerable.Range(0, 24).Select(player => $"0.000 player {player}: to everyone"), "0.000 print: true\t2.0\t2"],
            simulation.Log);
    }

    // facts, "Errors and callbacks": a script that replaces a global native changes what BJ functions call
    // (ForGroupBJ used a replaced ForGroup)
    [InstallFact]
    public void A_replaced_native_is_what_a_BJ_function_calls()
    {
        using var simulation = Load("""
            function ForGroup(whichGroup, callback)
              print("replaced", whichGroup == bj_lastCreatedGroup)
              callback()
            end
            ForGroupBJ(bj_lastCreatedGroup, function() print("called back") end)
            DisplayTimedTextToPlayer = function(toPlayer, x, y, duration, message)
              if toPlayer == Player(0) then print("shown", message) end
            end
            BJDebugMsg("hello")
            """);

        Assert.Equal(["0.000 print: replaced\ttrue", "0.000 print: called back", "0.000 print: shown\thello"], simulation.Log);
    }
}
