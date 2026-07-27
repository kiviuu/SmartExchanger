using SkiaSharp;
using SmartExchanger.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ApplyOpacityMaskNodeViewModel : BaseNodeViewModel
    {
        private readonly IShaderService _shaderService;
        public ConnectorViewModel TextureInputConnector { get; }
        public ConnectorViewModel MaskInputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }

        public ApplyOpacityMaskNodeViewModel(IShaderService shaderService)
        {
            this._shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));
            Title = "Apply Opacity Mask";
            TextureInputConnector = new ConnectorViewModel(this, "Texture");
            MaskInputConnector = new ConnectorViewModel(this, "Mask");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(TextureInputConnector);
            Inputs.Add(MaskInputConnector);
            Outputs.Add(OutputConnector);

            this._shaderService.CreateCompiledShader(Shaders.Shader.ApplyOpacityMask);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            SKImage? inputImage = inputs.Get(TextureInputConnector);
            SKImage? maskImage = inputs.Get(MaskInputConnector);
            if (inputImage is null || maskImage is null)
            {
                return null;
            }

            using SKSurface surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            SKRuntimeEffect effect = _shaderService.GetCompiledShader(Shaders.Shader.ApplyOpacityMask);

            var textureShader = inputImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            var maskShader = maskImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);

            using var uniforms = new SKRuntimeEffectUniforms(effect);
            using var children = new SKRuntimeEffectChildren(effect)
            {
                ["inputImage"] = textureShader,
                ["inputMask"] = maskShader
            };

            var shader = effect.ToShader(uniforms, children);
            using var paint = new SKPaint
            {
                Shader = shader,
                BlendMode = SKBlendMode.Src
            };
            var destination = new SKRect(0, 0, size, size);
            canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
