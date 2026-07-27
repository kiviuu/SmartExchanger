using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SmartExchanger.Services;
using SmartExchanger.Shaders;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class AlphaToMaskNodeViewModel : BaseNodeViewModel
    {
        private readonly IShaderService _shaderService;

        [ObservableProperty]
        private float _strength = 0.0f;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }


        public AlphaToMaskNodeViewModel(IShaderService shaderService)
        {
            this._shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));
            Title = "Alpha To Mask";
            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);

            this._shaderService.CreateCompiledShader(Shader.AlphaToMask);
        }
        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var destination = new SKRect(0, 0, size, size);
            var input = inputs.Get(InputConnector);
            if (input is not null)
            {
                canvas.DrawImage(input, destination, new SKSamplingOptions());
            }
            SKRuntimeEffect effect = _shaderService.GetCompiledShader(Shader.AlphaToMask);
            using var uniforms = new SKRuntimeEffectUniforms(effect);
            using var inputShader = input?.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var children = new SKRuntimeEffectChildren(effect)
            {
                ["inputImage"] = inputShader
            };
            using var shader = effect.ToShader(uniforms, children);
            using var paint = new SKPaint
            {
                Shader = shader
            };
            canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
