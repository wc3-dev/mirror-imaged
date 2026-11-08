namespace MirrorImaged.Natives;

// What main and InitBlizzard set for the view, Recorded as the view isn't simulated, and the fog flags (probe cameras-4).
public sealed partial class GameNatives
{
    const string Cameras4 = "docs/facts.md, Cameras: probe cameras-4 (11-08)";

    bool _fog = true, _fogMask = true;

    /// <summary>Kept for IsFogEnabled; what it does to what a player sees is wave 6's.</summary>
    [Native(Level.Close)]
    public void FogEnable(bool enable) => _fog = enable;

    [Native(Level.Exact, Fact = Cameras4)]
    public bool IsFogEnabled() => _fog;

    /// <summary>Kept for IsFogMaskEnabled; what it does to what a player sees is wave 6's.</summary>
    [Native(Level.Close)]
    public void FogMaskEnable(bool enable) => _fogMask = enable;

    [Native(Level.Exact, Fact = Cameras4)]
    public bool IsFogMaskEnabled() => _fogMask;

    [Native(Level.Recorded, Reason = "The lighting models of the day and night; no native reads them back")]
    public void SetDayNightModels(string? terrainDNCFile, string? unitDNCFile) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFog(float a, float b, float c, float d, float e) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFogEx(int style, float zstart, float zend, float density, float red, float green, float blue) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFogExV(int style, float zstart, float zend, float density, float heightStart, float heightEnd, float linearStart, float linearEnd, float red, float green, float blue) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void ResetTerrainFog() { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetUnitFog(float a, float b, float c, float d, float e) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogColor(float red, float green, float blue) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogDensity(float density) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogDrawOverSky(bool drawOverSky) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogHeightEnd(float heightEnd) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogHeightStart(float heightStart) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogLinearEnd(float linearEnd) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogLinearStart(float linearStart) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogMaxLinearDensity(float maxLinearDensity) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogStyle(fogstyle? style) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogZEnd(float ZEnd) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogZStart(float zStart) { }

    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void EnableWorldFogBoundary(bool b) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetWaterBaseColor(int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetWaterDeforms(bool val) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetHDWaterParams(int red, int green, int blue, bool useColor, int vertexDisplacement, int minOpacity, int maxOpacity, int reflectivity, int emissivity, int edgeSoftness, int waveStrength) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetHDWaterParamsEx(int red, int green, int blue, bool @override, int vertexDisplacement, int minOpacity, int maxOpacity, int reflectivity, int emissivity, int edgeSoftness, int waveStrength, int envMapStrength) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterColor(int red, int green, int blue) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterColorOverride(bool @override) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEdgeSoftness(int edgeSoftness) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEmissivity(int emissivity) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEnvMapStrength(int envMapStrengthy) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterMaxOpacity(int maxOpacity) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterMinOpacity(int minOpacity) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterReflectivity(int reflectivity) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterVertexDisplacement(int vertexDisplacement) { }

    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterWaveStrength(int waveStrength) { }

    [Native(Level.Recorded, Reason = "The sky in the view; no native reads it back")]
    public void SetSkyModel(string? skyModelFile) { }

    [Native(Level.Recorded, Reason = "The sky in the view; no native reads it back")]
    public void BlzShowSkyBox(bool show) { }

    [Native(Level.Recorded, Reason = "Whether the terrain is drawn; no native reads it back")]
    public void BlzShowTerrain(bool show) { }
}
