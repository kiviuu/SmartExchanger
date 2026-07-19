using SkiaSharp;
using SmartExchanger.Shaders;

namespace SmartExchanger.Services
{
    public interface IShaderService
    {
        void ClearCompiledShaders();
        void CreateCompiledShader(Shader shader);
        SKRuntimeEffect GetCompiledShader(Shader shader);
        bool RemoveCompiledShader(Shader shader);
    }
}