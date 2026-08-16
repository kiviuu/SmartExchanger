using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Shaders;

namespace SmartExchanger.Rendering.MaterialPreview.Triplanar
{
    public sealed class TriplanarEffectsManager : DefaultEffectsManager
    {
        public TriplanarEffectsManager()
        {
            RegisterTriplanarPasses();
        }

        private void RegisterTriplanarPasses()
        {
            IRenderTechnique meshTechnique = GetTechnique(DefaultRenderTechniqueNames.Mesh) ?? throw new InvalidOperationException("HelixToolkit Mesh render technique was not found.");

            meshTechnique.AddPass(
                    new ShaderPassDescription(TriplanarPassName.Pbr)
                    {
                        ShaderList = [ DefaultVSShaderDescriptions.VSMeshDefault, TriplanarShaderDescription.Pbr ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                    }
                );

            meshTechnique.AddPass(
                    new ShaderPassDescription(TriplanarPassName.PbrOit)
                    {
                        ShaderList = [ DefaultVSShaderDescriptions.VSMeshDefault, TriplanarShaderDescription.PbrOit ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                    }
                );

            meshTechnique.AddPass(
                    new ShaderPassDescription(TriplanarPassName.PbrOitDepthPeeling)
                    {
                        ShaderList = [ DefaultVSShaderDescriptions.VSMeshDefault, TriplanarShaderDescription.PbrOitDepthPeeling ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                    }
                );
        }
    }
}
