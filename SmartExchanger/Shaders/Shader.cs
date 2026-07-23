using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.Shaders
{
    public enum Shader
    {
        Threshold,
        Invert,
        WorleyNoise,
        HeightToNormal,
        PackRoughnessMetallic,
        PackBaseColorOpacity
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
                _ => shader.ToString()
            };
        }
    }
}
