namespace SmartExchanger.Options
{
    public sealed class RenderingOptions
    {
        public const string SectionName = "Rendering";

        public int DefaultTextureSize { get; set; } = 512;
        public int OutputPreviewSize { get; set; } = 256;
        public int TexturePreviewSize { get; set; } = 512;
        public int MaterialPreviewSize { get; set; } = 512;
        public int MinimumGpuCacheMb { get; set; } = 16;
        public int MaximumGpuCacheMb { get; set; } = 96;
    }
}