using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SmartExchanger.Services;
using SmartExchanger.Shaders;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ThresholdNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _threshold = 0.5f;

        [ObservableProperty]
        private float _softness = 0.5f;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }
        private IShaderService _shaderService;
        public ThresholdNodeViewModel(IShaderService shaderService)
        {
            this._shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));

            Title = "Threshold Node";
            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);

            _shaderService.CreateCompiledShader(Shader.Threshold);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var destination = new SKRect(0,0,size,size);
            var input = inputs.Get(InputConnector);

            if (input is not null)
            {
                canvas.DrawImage(input, destination, new SKSamplingOptions());
            }
            SKRuntimeEffect effect = _shaderService.GetCompiledShader(Shader.Threshold);
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["threshold"] = Math.Clamp(Threshold, 0.0f, 1.0f),
                ["softness"] = Math.Clamp(Softness, 0.0f, 1.0f)
            };
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
