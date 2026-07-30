#define MESH
#define PBR
#define CLEARCOAT

#include "psMeshPBRTriplanarCommon.hlsl"

PSOITOutput main(PSInput input)
{
    float4 color = EvaluateTriplanarPBR(input);
    return calculateOIT(color, input.vEye.w, input.p.z);
}
