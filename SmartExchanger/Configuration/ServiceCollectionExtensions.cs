using Microsoft.Extensions.Configuration;
using SmartExchanger.Models;
using SmartExchanger.Options;

namespace SmartExchanger.Configuration
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ApplicationOptions>()
                .Bind(configuration.GetRequiredSection(ApplicationOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "Application name cannot be empty.")
                .Validate(options => options.SplashMinimumDisplayMilliseconds is >= 0 and <= 10_000, "Splash display time must be between 0 and 10000 milliseconds.")
                .ValidateOnStart();

            services.AddOptions<RenderingOptions>()
                .Bind(configuration.GetRequiredSection(RenderingOptions.SectionName))
                .Validate(options => IsValidTextureSize(options.DefaultTextureSize), "Default texture size must be a positive power of two.")
                .Validate(options => IsValidPreviewSize(options.OutputPreviewSize), "Output preview size is invalid.")
                .Validate(options => IsValidPreviewSize(options.TexturePreviewSize), "Texture preview size is invalid.")
                .Validate(options => IsValidPreviewSize(options.MaterialPreviewSize), "Material preview size is invalid.")
                .Validate(options => options.MinimumGpuCacheMb > 0, "Minimum GPU cache must be greater than zero.")
                .Validate(options => options.MaximumGpuCacheMb >= options.MinimumGpuCacheMb, "Maximum GPU cache must be greater than or equal to the minimum GPU cache.")
                .ValidateOnStart();

            services.AddOptions<MaterialPreviewOptions>()
                .Bind(configuration.GetRequiredSection(MaterialPreviewOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.EnvironmentMapsDirectory), "Environment maps directory cannot be empty.")
                .Validate(options => IsNormalized(options.DefaultRoughness), "Default roughness must be between 0 and 1.")
                .Validate(options => IsNormalized(options.DefaultMetallic), "Default metallic must be between 0 and 1.")
                .Validate(options => IsNormalized(options.AmbientOcclusion), "Ambient occlusion must be between 0 and 1.")
                .Validate(options => Enum.IsDefined(typeof(MaterialMappingMode), options.DefaultMappingMode), "Default material mapping mode is invalid.")
                .Validate(options => float.IsFinite(options.TriplanarScale) && options.TriplanarScale is >= 0.1f and <= 16.0f, "Triplanar scale must be between 0.1 and 16.")
                .Validate(options => float.IsFinite(options.TriplanarBlendSharpness) && options.TriplanarBlendSharpness is >= 1.0f and <= 16.0f,"Triplanar blend sharpness must be between 1 and 16.")
                .Validate(options => float.IsFinite( options.TriplanarNormalStrength) && options.TriplanarNormalStrength is >= 0.0f and <= 4.0f, "Triplanar normal strength must be between 0 and 4.")
                .ValidateOnStart();

            services.AddOptions<ShaderOptions>()
                .Bind(configuration.GetRequiredSection(ShaderOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Directory), "Shader directory cannot be empty.")
                .ValidateOnStart();

            services.AddOptions<ExportOptions>()
                .Bind(configuration.GetRequiredSection(ExportOptions.SectionName))
                .Validate(options => options.JpegQuality is >= 1 and <= 100, "JPEG quality must be between 1 and 100.")
                .ValidateOnStart();

            services.AddOptions<UndoRedoOptions>()
                .Bind(configuration.GetRequiredSection(UndoRedoOptions.SectionName))
                .Validate(options => options.Capacity is >= 1 and <= 500, "Capacity must be between 1 and 500.")
                .Validate(options => options.CommitDelayMilliseconds is >= 0 and <= 5000, "Commit delay must be between 0 and 5000 milliseconds.")
                .ValidateOnStart();

            return services;
        }



        private static bool IsValidPreviewSize(int value)
        {
            return value is >= 64 and <= 4096 && IsPowerOfTwo(value);
        }

        private static bool IsValidTextureSize(int value)
        {
            return value is >= 128 and <= 4096 && IsPowerOfTwo(value);
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private static bool IsNormalized(float value)
        {
            return float.IsFinite(value) && value is >= 0f and <= 1f;
        }
    }
}
