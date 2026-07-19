using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.Shaders
{
    public enum Shader
    {
        Threshold,
        Invert
    }


    public static class ShadersExtensions
    {
        public static string ToShaderString(this Shader shader)
        {
            return shader switch
            {
                Shader.Threshold => "Threshold",
                Shader.Invert => "Invert",
                _ => shader.ToString()
            };
        }
    }
}
