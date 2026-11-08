using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A fact that reads the Warcraft III install on this computer; skipped when there is none.</summary>
public sealed class InstallFactAttribute : FactAttribute
{
    public InstallFactAttribute() => Skip = NoInstall();

    /// <summary>Why a test that reads the install is skipped, when there is none.</summary>
    internal static string? NoInstall() =>
        GameInstall.Find() is null ? $"No Warcraft III install in {GameInstall.Place()}; set {GameInstall.Variable} to run it" : null;
}

/// <summary>A theory that reads the Warcraft III install on this computer; skipped when there is none.</summary>
public sealed class InstallTheoryAttribute : TheoryAttribute
{
    public InstallTheoryAttribute() => Skip = InstallFactAttribute.NoInstall();
}
