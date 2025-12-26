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