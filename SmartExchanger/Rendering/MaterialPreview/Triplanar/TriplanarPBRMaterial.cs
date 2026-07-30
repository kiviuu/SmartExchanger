using HelixToolkit.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX;
using System.Windows;

namespace SmartExchanger.Rendering.MaterialPreview.Triplanar;

/// <summary>
/// WPF wrapper that always creates a TriplanarPBRMaterialCore.
/// This is important because Material is a WPF Freezable and may be cloned.
/// </summary>
public sealed class TriplanarPBRMaterial : PBRMaterial
{
    protected override MaterialCore OnCreateCore()
    {
        return new TriplanarPBRMaterialCore
        {
            Name = Name,
            AlbedoColor = AlbedoColor,
            EmissiveColor = EmissiveColor,
            MetallicFactor = (float)MetallicFactor,
            RoughnessFactor = (float)RoughnessFactor,
            AmbientOcclusionFactor = (float)AmbientOcclusionFactor,
            ReflectanceFactor = (float)ReflectanceFactor,
            ClearCoatStrength = (float)ClearCoatStrength,
            ClearCoatRoughness = (float)ClearCoatRoughness,

            AlbedoMap = AlbedoMap,
            NormalMap = NormalMap,
            EmissiveMap = EmissiveMap,
            RoughnessMetallicMap = RoughnessMetallicMap,
            AmbientOcculsionMap = AmbientOcculsionMap,
            IrradianceMap = IrradianceMap,
            DisplacementMap = DisplacementMap,

            SurfaceMapSampler = SurfaceMapSampler,
            IBLSampler = IBLSampler,
            DisplacementMapSampler = DisplacementMapSampler,

            RenderAlbedoMap = RenderAlbedoMap,
            RenderNormalMap = RenderNormalMap,
            RenderEmissiveMap = RenderEmissiveMap,
            RenderRoughnessMetallicMap = RenderRoughnessMetallicMap,
            RenderAmbientOcclusionMap = RenderAmbientOcclusionMap,
            RenderIrradianceMap = RenderIrradianceMap,
            RenderDisplacementMap = RenderDisplacementMap,
            RenderEnvironmentMap = RenderEnvironmentMap,
            RenderShadowMap = RenderShadowMap,

            EnableAutoTangent = EnableAutoTangent,
            EnableTessellation = EnableTessellation,
            EnableFlatShading = EnableFlatShading,

            DisplacementMapScaleMask = DisplacementMapScaleMask,
            UVTransform = UVTransform,
            MaxDistanceTessellationFactor = (float)MaxDistanceTessellationFactor,
            MinDistanceTessellationFactor = (float)MinDistanceTessellationFactor,
            MaxTessellationDistance = (float)MaxTessellationDistance,
            MinTessellationDistance = (float)MinTessellationDistance,
            VertexColorBlendingFactor = (float)VertexColorBlendingFactor
        };
    }

    protected override Freezable CreateInstanceCore()
    {
        return new TriplanarPBRMaterial();
    }
}
