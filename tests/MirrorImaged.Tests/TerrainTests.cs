using MirrorImaged.Data;
using War3Net.Build.Environment;

namespace MirrorImaged.Tests;

/// <summary>The ground's height from <c>war3map.w3e</c>, by the rules game-facts' "Units" measures for <c>GetLocationZ</c>.</summary>
public sealed class TerrainTests
{
    // A map of 5 by 5 corners from (-256, -256), flat at cliff level 2, with the corners changed.
    static Terrain Ground(params (int X, int Y, Action<TerrainTile> Change)[] corners)
    {
        var environment = new MapEnvironment(MapEnvironmentFormatVersion.v11) { Width = 5, Height = 5, Left = -256, Bottom = -256 };
        for (var i = 0; i < 25; i++) environment.TerrainTiles.Add(new TerrainTile { Height = 0, CliffLevel = 2 });
        foreach (var (x, y, change) in corners) change(environment.TerrainTiles[y * 5 + x]);
        return new Terrain(environment);
    }

    [Fact]
    public void A_corner_is_its_height_and_cliff_level_times_128()
    {
        var ground = Ground((2, 2, corner => (corner.Height, corner.CliffLevel) = (0.5f, 3)));

        Assert.Equal(192, ground.Height(0, 0));
        Assert.Equal(0, ground.Height(128, 0));
        Assert.Equal(0, ground.Height(-256, -256));
    }

    // facts: inside a tile the bilinear blend of its four corners, not two triangles
    [Fact]
    public void Inside_a_tile_the_height_is_the_bilinear_blend_of_its_corners()
    {
        var ground = Ground((2, 2, corner => corner.CliffLevel = 3));

        Assert.Equal(32, ground.Height(64, 64));
        Assert.Equal(64, ground.Height(0, 64));
        Assert.Equal(96, ground.Height(32, 0));
    }

    // facts: water at its surface x 128 - 89.6 where that is above the ground; the map's edge reads the ground
    [Fact]
    public void Water_above_the_ground_is_its_surface_but_at_the_maps_edge()
    {
        var ground = Ground(
            (2, 2, corner => (corner.IsWater, corner.WaterHeight) = (true, 1)),
            (1, 2, corner => (corner.IsWater, corner.WaterHeight, corner.Height) = (true, 0.5f, 1)),
            (0, 2, corner => (corner.IsWater, corner.WaterHeight) = (true, 1)));

        Assert.Equal(38.4f, ground.Height(0, 0), 3);
        Assert.Equal(128, ground.Height(-128, 0));
        Assert.Equal(0, ground.Height(-256, 0));
    }

    // facts: a ramp corner reads 32 higher for each of the four tiles around it with a ramp corner one level higher:
    // 64 with one beside it, 32 with one only diagonal, 96 with an L of them
    [Theory]
    [InlineData(new[] { 3 }, new[] { 2 }, 64)]
    [InlineData(new[] { 3 }, new[] { 3 }, 32)]
    [InlineData(new[] { 3, 2 }, new[] { 2, 3 }, 96)]
    [InlineData(new[] { 1 }, new[] { 2 }, 64)]
    public void A_ramp_corner_rises_by_the_tiles_with_a_ramp_corner_a_level_above(int[] xs, int[] ys, int raised)
    {
        var corners = xs.Zip(ys, (x, y) => (x, y, (Action<TerrainTile>)(corner => (corner.IsRamp, corner.CliffLevel) = (true, 3))))
            .Append((2, 2, corner => corner.IsRamp = true));

        Assert.Equal(raised, Ground([.. corners]).Height(0, 0));
    }

    [Fact]
    public void A_ramp_corner_a_level_above_that_isnt_a_ramp_raises_nothing()
    {
        Assert.Equal(0, Ground((2, 2, corner => corner.IsRamp = true), (3, 2, corner => corner.CliffLevel = 3)).Height(0, 0));
    }

    [Fact]
    public void A_point_off_the_map_reads_its_nearest_edge()
    {
        var ground = Ground((4, 4, corner => corner.Height = 1));

        Assert.Equal(128, ground.Height(1000, 1000));
        Assert.Equal(0, ground.Height(-1000, 1000));
    }
}
