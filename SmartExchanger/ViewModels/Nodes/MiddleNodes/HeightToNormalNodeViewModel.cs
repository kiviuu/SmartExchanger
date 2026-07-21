using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SmartExchanger.Services;
using SmartExchanger.Shaders;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class HeightToNormalNodeViewModel : BaseNodeViewModel
    {
        private readonly IShaderService _shaderService;

        [ObservableProperty]
        private float _strength = 4.0f;

        [ObservableProperty]
        private bool _invertY = false;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }
        public HeightToNormalNodeViewModel(IShaderService shaderService)
        {
            this._shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));
            Title = "Height To Normal";
            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);
            this._shaderService.CreateCompiledShader(Shaders.Shader.HeightToNormal);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            var input = inputs.Get(InputConnector);
            if (input is null)
            {
                return null;
            }

            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var destination = new SKRect(0, 0, size, size);

            SKRuntimeEffect effect = _shaderService.GetCompiledShader(Shader.HeightToNormal);
            using var inputShader = input.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var unifroms = new SKRuntimeEffectUniforms(effect)
            {
                ["strength"] = Strength,
                ["invertY"] = InvertY ? 1f : 0f,
                ["resolution"] = new SKPoint(size, size)
            };
            using var children = new SKRuntimeEffectChildren(effect)
            {
                ["inputImage"] = inputShader
            };
            using var shader = effect.ToShader(unifroms, children);
            using var paint = new SKPaint
            {
                Shader = shader,
                BlendMode = SKBlendMode.Src
            };
            surface.Canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
