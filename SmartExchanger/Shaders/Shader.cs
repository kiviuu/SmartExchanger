namespace SmartExchanger.Shaders
{
    public enum Shader
    {
        Threshold,
        Invert,
        WorleyNoise,
        HeightToNormal,
        PackRoughnessMetallic,
        PackBaseColorOpacity,
        AlphaToMask,
        ApplyOpacityMask
    }


    public static class ShadersExtensions
    {
        public static string ToShaderString(this Shader shader)
        {
            return shader switch
            {
                Shader.Threshold => "Threshold",
                Shader.Invert => "Invert",
                Shader.WorleyNoise => "WorleyNoise",
                Shader.HeightToNormal => "HeightToNormal",
                Shader.PackRoughnessMetallic => "PackRoughnessMetallic",
                Shader.PackBaseColorOpacity => "PackBaseColorOpacity",
                Shader.AlphaToMask => "AlphaToMask",
                Shader.ApplyOpacityMask => "ApplyOpacityMask",
                _ => shader.ToString()
            };
        }
    }
}
