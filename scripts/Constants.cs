public static class Constants
{
    public static int ObstaclesLayer => 2;
    public static int SelectableLayer => 3;

    public static uint AsMask(int layerNumber) => 1u << (layerNumber - 1);
}