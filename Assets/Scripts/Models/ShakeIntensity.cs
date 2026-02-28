/// <summary>
/// SHAKEINTENSITY - Camera shake intensity presets.
/// 
/// PURPOSE:
/// Provides tile-size-relative shake intensity values
/// for camera shake effects.
/// 
/// PRESETS:
/// - High: TileSize / 6 (strong impact)
/// - Medium: TileSize / 12 (normal hit)
/// - Low: TileSize / 24 (light shake)
/// - Stop: 0 (no shake)
/// 
/// USAGE:
/// ```csharp
/// ShakeIntensity.Initialize(tileSize);
/// CameraManager.Shake(ShakeIntensity.High);
/// ```
/// 
/// RELATED FILES:
/// - CameraManager.cs: Uses for shake effects
/// - AttackHelper.cs: Triggers shake on hits
/// </summary>
public static class ShakeIntensity
{
    private static float TileSize = 1f;

    public static float High { get; private set; }
    public static float Medium { get; private set; }
    public static float Low { get; private set; }
    public static float Stop { get; private set; } = 0f;

    public static void Initialize(float tileSize)
    {
        TileSize = tileSize;
        High = TileSize / 6f;
        Medium = TileSize / 12f;
        Low = TileSize / 24f;
    }
}