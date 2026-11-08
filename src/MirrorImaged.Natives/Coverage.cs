using System.Reflection;
using System.Text;

namespace MirrorImaged.Natives;

/// <summary>How many natives are at each level, and which are Missing.</summary>
public sealed class Coverage
{
    Coverage(IReadOnlyDictionary<Level, int> counts, IReadOnlyList<string> missing)
    {
        Counts = counts;
        Missing = missing;
    }

    /// <summary>The number of natives at each level, every level included.</summary>
    public IReadOnlyDictionary<Level, int> Counts { get; }

    /// <summary>The Missing natives' names, sorted.</summary>
    public IReadOnlyList<string> Missing { get; }

    /// <summary>The coverage of the natives <paramref name="natives"/> declares: its methods marked <see cref="NativeAttribute"/>.</summary>
    public static Coverage Of(Type natives)
    {
        var levels = natives.GetMethods()
            .Select(method => (method.Name, method.GetCustomAttribute<NativeAttribute>()?.Level))
            .Where(native => native.Level is not null)
            .ToList();
        return new Coverage(
            Enum.GetValues<Level>().ToDictionary(level => level, level => levels.Count(native => native.Level == level)),
            levels.Where(native => native.Level == Level.Missing).Select(native => native.Name).Order(StringComparer.Ordinal).ToList());
    }

    /// <summary>The counts, one level a line, then the Missing natives, one a line.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        foreach (var level in Enum.GetValues<Level>())
            text.Append($"{level,-8}{Counts[level],5}\n");
        text.Append($"{"All",-8}{Counts.Values.Sum(),5}\n\nMissing:\n");
        foreach (var name in Missing)
            text.Append(name).Append('\n');
        return text.ToString();
    }
}
