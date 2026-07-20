using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SmartExchanger.Services;
using SmartExchanger.Shaders;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class WorleyNoiseNodeViewModel : BaseNodeViewModel
    {
        private readonly IShaderService _shaderService;

        [ObservableProperty]
        private float _scale = 8.0f;

        [ObservableProperty]
        private float _jitter = 1.0f;

        [ObservableProperty]
        private float _seed = 0f;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector{ get; }

        public WorleyNoiseNodeViewModel(IShaderService shaderService)
        {
            this._shaderService = shaderService;
            Title = "Worley Noise";
            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);

            this._shaderService.CreateCompiledShader(Shader.WorleyNoise);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var destination = new SKRect(0, 0, size, size);
            var input = inputs.Get(InputConnector);
            //if (input is not null)
            //{
            //    canvas.DrawImage(input, destination, new SKSamplingOptions());
            //}
            SKRuntimeEffect effect = _shaderService.GetCompiledShader(Shader.WorleyNoise);
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["resolution"] = new SKPoint(size, size),
                ["scale"] = Scale,
                ["jitter"] = Jitter,
                ["seed"] = Seed
            };
            using var inputShader = input?.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var fallbackShader = SKShader.CreateColor(SKColors.White);
            SKShader childShader = inputShader ?? fallbackShader;
            using var children = new SKRuntimeEffectChildren(effect)
            {
                ["inputImage"] = childShader
            };
            using var shader = effect.ToShader(uniforms, children);

            using var paint = new SKPaint
            {
                Shader = shader,
                BlendMode = SKBlendMode.SrcOver
            };

            canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
