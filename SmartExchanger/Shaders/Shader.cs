using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.Shaders
{
    public enum Shader
    {
        Threshold
    }


    public static class ShadersExtensions
    {
        public static string ToShaderString(this Shader shader)
        {
            return shader switch
            {
                Shader.Threshold => "Threshold",
                _ => shader.ToString()
            };
        }
    }
}
