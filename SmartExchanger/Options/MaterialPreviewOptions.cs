namespace SmartExchanger.Options
{
    public sealed class MaterialPreviewOptions
    {
        public const string SectionName = "MaterialPreview";
        public string EnvironmentMapsDirectory { get; set; } = "Assets/EnvironmentMaps";
        public string? DefaultEnvironmentMap { get; set; }
        public float DefaultRoughness { get; set; } = 0.5f;
        public float DefaultMetallic { get; set; } = 0.0f;
        public float AmbientOcclusion { get; set; } = 1.0f;

        public float TriplanarScale { get; set; } = 1.0f;
        public float TriplanarBlendSharpness { get; set; } = 4.0f;
        public float TriplanarNormalStrength { get; set; } = 1.0f;
    }
}