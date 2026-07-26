namespace SmartExchanger.Options
{
    public sealed class ExportOptions
    {
        public const string SectionName = "Export";

        public int JpegQuality { get; set; } = 95;
    }
}