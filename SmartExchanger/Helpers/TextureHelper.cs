using SkiaSharp;

namespace SmartExchanger.Helpers
{
    public class TextureHelper
    {
        public const int TextureSize = 256;

        public static SKBitmap CreateEmptyBitmap()
        {
            return new SKBitmap(TextureSize, TextureSize);
        }
    }
}
