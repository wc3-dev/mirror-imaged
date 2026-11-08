namespace MirrorImaged.Data;

/// <summary>The install's active build, from its <c>.build.info</c>.</summary>
/// <param name="Version">The game's version (<c>3.0.1.24371</c>).</param>
/// <param name="Key">The build key, which changes with every update of the game's files.</param>
public sealed record GameBuild(string Version, string Key);

/// <summary>
/// Where a Warcraft III install is: a folder given, else the folder <see cref="Variable"/> names, else the usual folder.
/// A folder holds an install when it has <c>.build.info</c>.
/// </summary>
public static class GameInstall
{
    /// <summary>The environment variable that names the install's folder when none is given.</summary>
    public const string Variable = "MIRROR_GAME";

    const string BuildInfo = ".build.info";

    /// <summary>The folder the game installs to by default.</summary>
    public static string UsualFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Warcraft III");

    /// <summary>
    /// Where the install is looked for: <paramref name="folder"/> when one is given, else the folder
    /// <see cref="Variable"/> names when it is set, else the usual folder.
    /// </summary>
    public static string Place(string? folder = null) => Place(folder, Environment.GetEnvironmentVariable(Variable));

    /// <summary>The install's folder (see <see cref="Place(string?)"/>); null when it holds none.</summary>
    public static string? Find(string? folder = null) => Place(folder) is var place && IsInstall(place) ? place : null;

    /// <summary>The install's folder (see <see cref="Place(string?)"/>).</summary>
    /// <exception cref="DirectoryNotFoundException">There is no install there; the message names the place.</exception>
    public static string Require(string? folder = null) =>
        Find(folder) ?? throw new DirectoryNotFoundException(
            $"No Warcraft III install (a folder with {BuildInfo}) in {Place(folder)}."
            + (string.IsNullOrWhiteSpace(folder) ? $" Set {Variable} to the game's folder." : ""));

    /// <summary>Whether <paramref name="folder"/> holds an install: it has <c>.build.info</c>.</summary>
    public static bool IsInstall(string folder) => File.Exists(Path.Combine(folder, BuildInfo));

    /// <summary>The active build of the install in <paramref name="folder"/>.</summary>
    /// <exception cref="InvalidDataException"><c>.build.info</c> names no active build.</exception>
    public static GameBuild Build(string folder)
    {
        var lines = File.ReadAllLines(Path.Combine(folder, BuildInfo));
        var head = lines[0].Split('|').Select(column => column.Split('!')[0]).ToArray();
        foreach (var line in lines.Skip(1))
        {
            var row = head.Zip(line.Split('|')).ToDictionary(pair => pair.First, pair => pair.Second);
            if (row.GetValueOrDefault("Active") == "1")
                return new GameBuild(row.GetValueOrDefault("Version", ""), row.GetValueOrDefault("Build Key", ""));
        }

        throw new InvalidDataException($"{Path.Combine(folder, BuildInfo)} names no active build");
    }

    internal static string Place(string? folder, string? variable) =>
        !string.IsNullOrWhiteSpace(folder) ? folder
        : !string.IsNullOrWhiteSpace(variable) ? variable
        : UsualFolder;
}
