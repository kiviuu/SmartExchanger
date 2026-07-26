using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.Models
{
    public sealed record TexturePreviewFrame(byte[]? Pixels, int Width, int Height, int RowBytes)
    {
        public static TexturePreviewFrame Empty { get; } = new(Pixels:null,Width:0,Height:0, RowBytes:0);

        // 4 bytes per pixels -> PBgra32
        public bool HasSignal => Pixels is not null && Width > 0 && Height > 0 && RowBytes >= Width * 4 && Pixels.Length >= RowBytes*Height;
    }
}
