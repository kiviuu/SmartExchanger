using SkiaSharp;
using SmartExchanger.Shaders;
using System.IO;
using Microsoft.Extensions.Options;
using SmartExchanger.Options;

namespace SmartExchanger.Services
{
    public class ShaderService : IShaderService, IDisposable
    {
        private Dictionary<Shader, SKRuntimeEffect> CompiledShaders = new();
        private readonly string _sharedDirectory;
        public ShaderService(IOptions<ShaderOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            string configuredDirectory = options.Value.Directory;
            this._sharedDirectory = Path.IsPathRooted(configuredDirectory) ? configuredDirectory :
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDirectory));
        }
        public void CreateCompiledShader(Shader shader)
        {
            if (!CompiledShaders.ContainsKey(shader))
            {
                string shaderName = shader.ToShaderString();
                string fileName = GetFileName(shaderName);
                string filePath = Path.Combine(_sharedDirectory, String.Concat(fileName, ".sksl"));
                bool shaderCodeFounded = File.Exists(filePath);
                if (!shaderCodeFounded)
                {
                    throw new FileNotFoundException("Shader file not found.", filePath);
                }

                var shaderCode = File.ReadAllText(filePath);
                if (shaderCode is null)
                {
                    throw new InvalidOperationException("Cannot read shader file.");
                }

                SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(shaderCode, out string errors);
                if (effect is null)
                {
                    throw new InvalidOperationException($"Sksl error {errors}");
                }

                this.CompiledShaders[shader] = effect;
            }
        }

        public SKRuntimeEffect GetCompiledShader(Shader shader)
        {
            if (!CompiledShaders.ContainsKey(shader))
            {
                this.CreateCompiledShader(shader);
            }
            return this.CompiledShaders[shader];
        }

        public bool RemoveCompiledShader(Shader shader)
        {
            if (!this.CompiledShaders.Remove(shader, out var effect))
            {
                return false;
            }
            effect.Dispose();
            return true;
        }

        public void ClearCompiledShaders()
        {
            foreach(var shader in this.CompiledShaders.Keys)
            {
                this.CompiledShaders[shader].Dispose();
            }
            this.CompiledShaders.Clear();
        }

        private string GetFileName(string shaderName)
        {
            string fileName = shaderName.ToLower();
            fileName = String.Concat(fileName[0].ToString().ToUpper(), fileName.Substring(1));
            return fileName;
        }

        public void Dispose()
        {
            ClearCompiledShaders();
        }
    }
}
