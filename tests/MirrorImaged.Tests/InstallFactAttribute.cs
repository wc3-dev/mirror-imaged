using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A fact that reads the Warcraft III install on this computer; skipped when there is none.</summary>
public sealed class InstallFactAttribute : FactAttribute
{
    public InstallFactAttribute()
    {
        if (GameInstall.Find() is null)
            Skip = $"No Warcraft III install in {GameInstall.Place()}; set {GameInstall.Variable} to run it";
    }
}
