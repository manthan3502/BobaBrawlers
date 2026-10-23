using UnityEngine;
namespace BobaBrawlers
{
    public static class Palette
    {
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
        public static readonly Color Blue = Hex("#3268F2"), Orange = Hex("#FF923A"), Lime = Hex("#A9DC45"),
            Purple = Hex("#965CDB"), Cream = Hex("#F7D6AC"), Navy = Hex("#18243F"), Pink = Hex("#FF6FB5");
    }
}
