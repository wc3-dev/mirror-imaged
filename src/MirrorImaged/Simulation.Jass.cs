using System.Text;
using MirrorImaged.Natives;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged;

// A JASS map runs as the Lua War3Net's transpiler makes of its war3map.j, with JASS's rules kept (decisions D130 to
// D137): the functions they call, and the operation limit.
public sealed partial class Simulation
{
    /// <summary>
    /// JASS's operation limit as Lua instructions: the game ends a JASS thread after 3,000,000 of its own operations
    /// (probe jass-threads), of which a Lua instruction stands for 1.4 to 3 by the code's shape, 1.82 on average over
    /// the probe's five loops (decisions D135).
    /// </summary>
    internal const int OperationLimit = 2_100_100;

    // The script made into Lua against common.j's natives and globals and blizzard.j, when given. A script that doesn't
    // parse or names what isn't declared is one the game wouldn't load; any other failure is MirrorImaged's.
    static byte[] JassLua(string path, byte[] script, JassCompilationUnitSyntax? blizzard)
    {
        try
        {
            return Encoding.UTF8.GetBytes(Jass.ToLua([CommonJ(), .. blizzard is null ? [] : new[] { blizzard }], Encoding.UTF8.GetString(script), rules: true));
        }
        catch (Exception e) when (e is InvalidDataException or KeyNotFoundException)
        {
            throw new InvalidDataException($"The game wouldn't load {path}: its war3map.j can't be made into Lua ({e.Message})", e);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new NotSupportedException($"MirrorImaged can't make {path}'s war3map.j into Lua, though the game may load it: {e.Message}", e);
        }
    }

    // The functions JASS's rules call; dividing -2147483648 by -1 closes the game (probe jass-divide-overflow).
    void AddJassRules()
    {
        _lua.Register("MirrorImaged_Closes", args => throw new GameClosedException((string)args[0]!));
        _lua.AddJassFunctions();
    }
}
