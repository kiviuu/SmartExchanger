using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Model;
using HelixToolkit.SharpDX.Shaders;

namespace SmartExchanger.Rendering.MaterialPreview.Triplanar
{
    internal sealed class TriplanarPBRMaterialVariable : PBRMaterialVariable
    {
        private readonly ShaderPass _opaquePass;
        private readonly ShaderPass _oitPass;
        private readonly ShaderPass _depthPeelingInitPass;
        private readonly ShaderPass _depthPeelingPass;

        public TriplanarPBRMaterialVariable(IEffectsManager effectsManager, IRenderTechnique technique, PBRMaterialCore material) 
            : base(effectsManager, technique, material, TriplanarPassName.Pbr)
        {
            _opaquePass = technique[TriplanarPassName.Pbr];
            _oitPass = technique[TriplanarPassName.PbrOit];
            _depthPeelingInitPass = technique[DefaultPassNames.OITDepthPeelingInit];
            _depthPeelingPass = technique[TriplanarPassName.PbrOitDepthPeeling];
        }

        public override ShaderPass GetPass(RenderType renderType, RenderContext context)
        {
            if (renderType != RenderType.Transparent)
            {
                return _opaquePass;
            }
            return context.OITRenderStage switch
            {
                OITRenderStage.SinglePassWeighted => _oitPass,
                OITRenderStage.DepthPeelingInitMinMaxZ => _depthPeelingInitPass,
                OITRenderStage.DepthPeeling => _depthPeelingPass,
                _ => _opaquePass
            };
        }
    }
}
