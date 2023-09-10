namespace WheelWatcher.Library
{
    internal class ControlIds
    {
        internal static string AllWheelTiles_XPath => "//div[@class='tile default-layout']//div[@class='results']/span/ancestor::div[@class='tile default-layout']";
        internal static string WheelTitle_XPath => ".//span[@class='game-title']";
        internal static string WheelResults_XPath => ".//div[@class='results']/span";
    }
}
