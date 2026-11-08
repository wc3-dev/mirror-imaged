using MirrorImaged.Data;

namespace MirrorImaged.Tests;

public sealed class GameInstallTests : IDisposable
{
    const string Header =
        "Branch!STRING:0|Active!DEC:1|Build Key!HEX:16|CDN Key!HEX:16|Tags!STRING:0|Version!STRING:0|Product!STRING:0";

    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void The_build_is_the_active_line_of_build_info()
    {
        File.WriteAllText(Path.Combine(_folder, ".build.info"),
            Header + "\n"
            + "us|0|00000000000000000000000000000000|0|Windows US? enUS text?|2.0.0.1|w3\n"
            + "eu|1|dd5e630e7a6ad6466075eb1c50e283af|0|Windows EU? enUS text?|3.0.1.24371|w3\n");

        Assert.Equal(new GameBuild("3.0.1.24371", "dd5e630e7a6ad6466075eb1c50e283af"), GameInstall.Build(_folder));
    }

    [Fact]
    public void Build_info_with_no_active_build_is_an_error()
    {
        File.WriteAllText(Path.Combine(_folder, ".build.info"), Header + "\nus|0|00|0||1.0|w3\n");

        var error = Assert.Throws<InvalidDataException>(() => GameInstall.Build(_folder));

        Assert.Contains("no active build", error.Message);
    }

    [Fact]
    public void The_install_is_looked_for_in_the_given_folder_then_the_variable_then_the_usual_folder()
    {
        Assert.Equal(@"C:\given", GameInstall.Place(@"C:\given", @"C:\variable"));
        Assert.Equal(@"C:\variable", GameInstall.Place(null, @"C:\variable"));
        Assert.Equal(GameInstall.UsualFolder, GameInstall.Place(" ", ""));
    }

    [Fact]
    public void An_install_is_a_folder_with_build_info()
    {
        Assert.False(GameInstall.IsInstall(_folder));
        Assert.Null(GameInstall.Find(_folder));

        File.WriteAllText(Path.Combine(_folder, ".build.info"), "");

        Assert.True(GameInstall.IsInstall(_folder));
        Assert.Equal(_folder, GameInstall.Find(_folder));
    }

    [Fact]
    public void No_install_in_a_given_folder_is_an_error_that_names_the_folder()
    {
        var error = Assert.Throws<DirectoryNotFoundException>(() => GameInstall.Require(_folder));

        Assert.Contains(_folder, error.Message);
    }
}
