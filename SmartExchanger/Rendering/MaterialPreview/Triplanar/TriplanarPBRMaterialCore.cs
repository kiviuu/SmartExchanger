using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Model;

namespace SmartExchanger.Rendering.MaterialPreview.Triplanar;

internal sealed class TriplanarPBRMaterialCore : PBRMaterialCore
{
    public override MaterialVariable
        CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
    {
        return new TriplanarPBRMaterialVariable(manager, technique,this);
    }
}