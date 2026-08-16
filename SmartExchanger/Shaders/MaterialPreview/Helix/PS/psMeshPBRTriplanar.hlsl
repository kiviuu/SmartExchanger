#define MESH
#define PBR
#define CLEARCOAT

#include "psMeshPBRTriplanarCommon.hlsl"

float4 main(PSInput input) : SV_Target
{
    float4 result = EvaluateTriplanarPBR(input);

    // The opaque pass must not use texture alpha.
    return float4(result.rgb, 1.0f);
}
