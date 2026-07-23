namespace SmartExchanger.Models
{
    public sealed record MaterialPreviewFrame(byte[]? BaseColorPng, byte[]? NormalPng, byte[]? RoughnessMetallicPng, bool IsTransparent)
    {
        public static MaterialPreviewFrame Empty { get; } =
            new(BaseColorPng: null, NormalPng: null, RoughnessMetallicPng: null, IsTransparent: false);
    }
}