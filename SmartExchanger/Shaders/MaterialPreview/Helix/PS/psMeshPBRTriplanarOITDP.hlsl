#define MESH
#define PBR
#define CLEARCOAT

#include "psOITDepthPeelingCommon.hlsl"
#include "psMeshPBRTriplanarCommon.hlsl"

DDPOutputMRT main(PSInput input)
{
    float4 color = EvaluateTriplanarPBR(input);
    return depthPeelPS(input.p, color);
}
