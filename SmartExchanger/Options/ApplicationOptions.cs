namespace SmartExchanger.Options
{
    public sealed class ApplicationOptions
    {
        public const string SectionName = "Application";

        public string Name { get; set; } = "Smart Exchanger";
        public string Subtitle { get; set; } = "Procedural Texture and Material Editor";
        public string Author { get; set; } = "Unknown author";
        public string CopyrightOwner { get; set; } = "Smart Exchanger";
        public int SplashMinimumDisplayMilliseconds { get; set; } = 2000;
    }
}