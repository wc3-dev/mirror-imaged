using System.Globalization;
using MirrorImaged;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

if (args is ["coverage"])
{
    Console.Write(Coverage.Of(typeof(GameNatives)));
    return 0;
}

if (args is ["run", var map, .. var options])
{
    double seconds = 60;
    int seed = 0;
    for (int i = 0; i < options.Length; i += 2)
    {
        var value = i + 1 < options.Length ? options[i + 1] : "";
        var known = options[i] switch
        {
            "--seconds" => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0,
            "--seed" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed),
            _ => false,
        };
        if (!known) return Usage();
    }

    try
    {
        using var simulation = Simulation.Load(map, new SimulationOptions { Seed = seed });
        try { simulation.Advance(TimeSpan.FromSeconds(seconds)); }
        catch (LuaException) when (simulation.Stopped is not null) { }
        foreach (var line in simulation.Log) Console.Write(line + "\n");
        if (simulation.Stopped is null) return 0;
        Console.Error.Write(simulation.Stopped.Message + "\n");
        return 1;
    }
    catch (Exception e)
    {
        Console.Error.Write(e.Message + "\n");
        return 1;
    }
}

return Usage();

static int Usage()
{
    Console.Error.Write("""
        usage:
          mirror coverage                                  How many natives are at each trust level, and which are Missing
          mirror run <map> [--seconds <n>] [--seed <n>]    Runs a Lua map for n simulated seconds (60) with a seed (0),
                                                           then prints its log

        """);
    return 2;
}
