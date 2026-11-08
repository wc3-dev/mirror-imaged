using War3Net.Build.Environment;

namespace MirrorImaged.Data;

/// <summary>
/// The ground of a map's <c>war3map.w3e</c> as <c>GetLocationZ</c> reads it (game-facts, "Units"; 15,625 of 15,625
/// corners of Lost Temple's playable area): at a corner of the 128 grid, its height and cliff level (height x 128 +
/// (cliff level - 2) x 128); water, at its surface x 128 - 89.6, where that is above the ground, but in the map's
/// outermost corners, which read the ground; a ramp corner 32 higher for each of the four tiles around it that has a
/// ramp corner one cliff level higher. Inside a tile, the bilinear blend of its four corners. Bridges and other
/// walkable destructables are not part of it: their decks stand on <see cref="Ground"/>.
/// </summary>
public sealed class Terrain
{
    const float Tile = 128;

    readonly int _width, _height;
    readonly float _left, _bottom;
    readonly float[] _corners, _ground;

    public Terrain(MapEnvironment environment)
    {
        // War3Net's Width and Height count tiles; the file holds a corner more each way
        (_width, _height, _left, _bottom) = ((int)environment.Width + 1, (int)environment.Height + 1, environment.Left, environment.Bottom);
        var tiles = environment.TerrainTiles;
        _corners = new float[tiles.Count];
        _ground = new float[tiles.Count];
        for (var y = 0; y < _height; y++)
            for (var x = 0; x < _width; x++)
            {
                var corner = tiles[y * _width + x];
                var ground = (corner.Height + corner.CliffLevel - 2) * Tile;
                if (corner.IsRamp)
                    for (var tileY = y - 1; tileY <= y; tileY++)
                        for (var tileX = x - 1; tileX <= x; tileX++)
                            if (HasRampAbove(tiles, tileX, tileY, corner.CliffLevel)) ground += 32;
                var water = corner.WaterHeight * Tile - 89.6f;
                var edge = x == 0 || y == 0 || x == _width - 1 || y == _height - 1;
                _ground[y * _width + x] = ground;
                _corners[y * _width + x] = corner.IsWater && !edge && water > ground ? water : ground;
            }
    }

    /// <summary>The map from its west and south edges to its east and north ones, as GetWorldBounds reads it.</summary>
    public (float MinX, float MinY, float MaxX, float MaxY) Bounds => (_left, _bottom, _left + (_width - 1) * Tile, _bottom + (_height - 1) * Tile);

    /// <summary>
    /// The ground's height at (<paramref name="x"/>, <paramref name="y"/>); a point off the map reads 0, but within an
    /// eighth of a unit past an edge, which reads its edge tile's blend carried on (probe terrain-edge: 0.1 past an
    /// edge carried on, 0.2 and further 0).
    /// </summary>
    public float Height(float x, float y) => Blend(_corners, x, y);

    /// <summary>As <see cref="Height"/>, but the ground beneath any water, on which a bridge's deck stands (game-facts, "Units").</summary>
    public float Ground(float x, float y) => Blend(_ground, x, y);

    float Blend(float[] corners, float x, float y)
    {
        double across = (x - _left) / Tile, up = (y - _bottom) / Tile;
        if (Off(across, _width) || Off(up, _height)) return 0;
        var (column, u) = Cell(across, _width);
        var (row, v) = Cell(up, _height);
        float At(int dx, int dy) => corners[(row + dy) * _width + column + dx];
        return (float)((1 - v) * ((1 - u) * At(0, 0) + u * At(1, 0)) + v * ((1 - u) * At(0, 1) + u * At(1, 1)));
    }

    // Past an edge by an eighth of a unit (1/1024 of a tile) or more; shortcut: the game's line is between 0.1 and 0.2
    static bool Off(double at, int corners) => at <= -1.0 / 1024 || at >= corners - 1 + 1.0 / 1024;

    // The tile a coordinate in tiles falls in, the edge's for one just past it, and how far across it the coordinate is
    static (int Tile, double Across) Cell(double at, int corners)
    {
        var tile = Math.Clamp((int)Math.Floor(at), 0, corners - 2);
        return (tile, at - tile);
    }

    bool HasRampAbove(List<TerrainTile> tiles, int tileX, int tileY, int level)
    {
        if (tileX < 0 || tileY < 0 || tileX >= _width - 1 || tileY >= _height - 1) return false;
        for (var y = tileY; y <= tileY + 1; y++)
            for (var x = tileX; x <= tileX + 1; x++)
                if (tiles[y * _width + x] is { IsRamp: true } corner && corner.CliffLevel == level + 1) return true;
        return false;
    }
}
