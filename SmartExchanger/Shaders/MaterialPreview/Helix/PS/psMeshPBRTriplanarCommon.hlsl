#ifndef PSMESHPBRTRIPLANARCOMMON_HLSL
#define PSMESHPBRTRIPLANARCOMMON_HLSL

#ifndef MESH
#define MESH
#endif

#ifndef PBR
#define PBR
#endif

#ifndef CLEARCOAT
#define CLEARCOAT
#endif

#include "..\Common\Common.hlsl"
#include "..\Common\DataStructs.hlsl"
#include "psCommon.hlsl"

struct TriplanarData
{
    float2 uvX;
    float2 uvY;
    float2 uvZ;
    float3 weights;
    float3 axisSigns;
};

float SignNotZero(float value)
{
    return value < 0.0f ? -1.0f : 1.0f;
}

float3 CalculateGeometricNormal(PSInput input)
{
    return bRenderFlat
        ? normalize(cross(ddy(input.wp.xyz), ddx(input.wp.xyz)))
        : normalize(input.n);
}

TriplanarData BuildTriplanarData(
    float3 worldPosition,
    float3 geometricNormal)
{
    TriplanarData result;

    float scale = max(displacementMapScaleMask.x, 0.0001f);
    float sharpness = max(displacementMapScaleMask.y, 1.0f);

    result.axisSigns = float3(
        SignNotZero(geometricNormal.x),
        SignNotZero(geometricNormal.y),
        SignNotZero(geometricNormal.z));

    float3 weights = pow(abs(geometricNormal), sharpness);
    weights /= max(weights.x + weights.y + weights.z, 0.0001f);
    result.weights = weights;

    // Projection along X: U = Z, V = Y.
    result.uvX = float2(
        worldPosition.z * -result.axisSigns.x,
        worldPosition.y) * scale;

    // Projection along Y: U = X, V = Z.
    result.uvY = float2(
        worldPosition.x * -result.axisSigns.y,
        worldPosition.z) * scale;

    // Projection along Z: U = X, V = Y.
    result.uvZ = float2(
        worldPosition.x * result.axisSigns.z,
        worldPosition.y) * scale;

    return result;
}

float4 SampleTriplanarAlbedo(TriplanarData data)
{
    return
        texDiffuseMap.Sample(samplerSurface, data.uvX) * data.weights.x +
        texDiffuseMap.Sample(samplerSurface, data.uvY) * data.weights.y +
        texDiffuseMap.Sample(samplerSurface, data.uvZ) * data.weights.z;
}

float3 SampleTriplanarRM(TriplanarData data)
{
    return
        texRMMap.Sample(samplerSurface, data.uvX).rgb * data.weights.x +
        texRMMap.Sample(samplerSurface, data.uvY).rgb * data.weights.y +
        texRMMap.Sample(samplerSurface, data.uvZ).rgb * data.weights.z;
}

float SampleTriplanarAO(TriplanarData data)
{
    return
        texAOMap.Sample(samplerSurface, data.uvX).r * data.weights.x +
        texAOMap.Sample(samplerSurface, data.uvY).r * data.weights.y +
        texAOMap.Sample(samplerSurface, data.uvZ).r * data.weights.z;
}

float3 SampleTriplanarEmissive(TriplanarData data)
{
    return
        texEmissiveMap.Sample(samplerSurface, data.uvX).rgb * data.weights.x +
        texEmissiveMap.Sample(samplerSurface, data.uvY).rgb * data.weights.y +
        texEmissiveMap.Sample(samplerSurface, data.uvZ).rgb * data.weights.z;
}

float3 DecodeNormalMap(float3 encodedNormal)
{
    float3 normal = encodedNormal * 2.0f - 1.0f;
    normal.xy *= max(displacementMapScaleMask.z, 0.0f);
    return normalize(normal);
}

float3 BuildProjectedTangent(
    float3 axis,
    float3 surfaceNormal)
{
    float3 tangent = axis - surfaceNormal * dot(axis, surfaceNormal);
    float lengthSquared = dot(tangent, tangent);

    if (lengthSquared > 0.000001f)
    {
        return tangent * rsqrt(lengthSquared);
    }

    float3 fallbackAxis = abs(surfaceNormal.y) < 0.999f
        ? float3(0.0f, 1.0f, 0.0f)
        : float3(1.0f, 0.0f, 0.0f);

    return normalize(cross(fallbackAxis, surfaceNormal));
}

float3 TransformProjectedNormal(
    float3 tangentSpaceNormal,
    float3 surfaceNormal,
    float3 projectionUAxis,
    float3 projectionVAxis)
{
    float3 tangent = BuildProjectedTangent(
        projectionUAxis,
        surfaceNormal);

    float3 bitangent =
        projectionVAxis -
        surfaceNormal * dot(projectionVAxis, surfaceNormal) -
        tangent * dot(projectionVAxis, tangent);

    float bitangentLengthSquared = dot(bitangent, bitangent);

    if (bitangentLengthSquared > 0.000001f)
    {
        bitangent *= rsqrt(bitangentLengthSquared);
    }
    else
    {
        bitangent = normalize(cross(surfaceNormal, tangent));
    }

    return normalize(
        tangent * tangentSpaceNormal.x +
        bitangent * tangentSpaceNormal.y +
        surfaceNormal * tangentSpaceNormal.z);
}

float3 SampleTriplanarNormal(
    TriplanarData data,
    float3 geometricNormal)
{
    float3 tangentNormalX = DecodeNormalMap(
        texNormalMap.Sample(samplerSurface, data.uvX).xyz);

    float3 tangentNormalY = DecodeNormalMap(
        texNormalMap.Sample(samplerSurface, data.uvY).xyz);

    float3 tangentNormalZ = DecodeNormalMap(
        texNormalMap.Sample(samplerSurface, data.uvZ).xyz);

    float3 worldNormalX = TransformProjectedNormal(
        tangentNormalX,
        geometricNormal,
        float3(0.0f, 0.0f, -data.axisSigns.x),
        float3(0.0f, 1.0f, 0.0f));

    float3 worldNormalY = TransformProjectedNormal(
        tangentNormalY,
        geometricNormal,
        float3(-data.axisSigns.y, 0.0f, 0.0f),
        float3(0.0f, 0.0f, 1.0f));

    float3 worldNormalZ = TransformProjectedNormal(
        tangentNormalZ,
        geometricNormal,
        float3(data.axisSigns.z, 0.0f, 0.0f),
        float3(0.0f, 1.0f, 0.0f));

    return normalize(
        worldNormalX * data.weights.x +
        worldNormalY * data.weights.y +
        worldNormalZ * data.weights.z);
}

float3 CalculateShadingNormal(
    TriplanarData data,
    float3 geometricNormal)
{
    return bHasNormalMap
        ? SampleTriplanarNormal(data, geometricNormal)
        : geometricNormal;
}

float3 LightSurface(
    in float4 wp,
    in float3 V,
    in float3 N,
    in float3 albedo,
    in float roughness,
    in float metallic,
    in float ambientOcclusion,
    in float reflectance,
    in float clearCoat,
    in float clearCoatRoughness)
{
    const float NdotV = saturate(dot(N, V));
    const float alpha = roughness * roughness;
    const float3 c_diff = lerp(albedo, float3(0, 0, 0), metallic) * ambientOcclusion;
    const float3 c_spec = 0.16 * reflectance * reflectance * (1 - metallic) + albedo * metallic;

#if defined(CLEARCOAT)
    clearCoatRoughness = lerp(0.089, 0.6, clearCoatRoughness);
    float clearCoatLinearRoughness = clearCoatRoughness * clearCoatRoughness;
#endif

    float3 acc_color = 0;

    for (int i = 0; i < NumLights; i++)
    {
        if (Lights[i].iLightType == 1)
        {
            const float3 L = normalize(Lights[i].vLightDir.xyz);
            const float3 H = normalize(L + V);
            const float NdotL = saturate(dot(N, L));
            const float LdotH = saturate(dot(L, H));
            const float NdotH = saturate(dot(N, H));
            float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
            float3 diffuse = c_diff * diffuse_factor;
            float3 specular = Specular_BRDF(alpha, c_spec, NdotV, NdotL, LdotH, NdotH, N, H);

#if defined(CLEARCOAT)
            float Dc = Filament_D_GGX(clearCoatLinearRoughness, NdotH, N, H);
            float Vc = V_Kelemen(LdotH);
            float Fc = Filament_F_Schlick(0.04, LdotH) * clearCoat;
            float Frc = (Dc * Vc) * Fc;
            acc_color += NdotL * Lights[i].vLightColor.rgb * ((diffuse + specular * (1 - Fc)) * (1 - Fc) + Frc);
#else
            acc_color += NdotL * Lights[i].vLightColor.rgb * (diffuse + specular);
#endif
        }
        else if (Lights[i].iLightType == 2)
        {
            float3 L = (float3)(Lights[i].vLightPos - wp);
            float dl = length(L);
            if (Lights[i].vLightAtt.w < dl)
            {
                continue;
            }

            L = L / dl;
            const float3 H = normalize(V + L);
            const float NdotL = saturate(dot(N, L));
            const float LdotH = saturate(dot(L, H));
            const float NdotH = saturate(dot(N, H));
            float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
            float3 diffuse = c_diff * diffuse_factor;
            float3 specular = Specular_BRDF(alpha, c_spec, NdotV, NdotL, LdotH, NdotH, N, H);
            float att = 1.0f / (Lights[i].vLightAtt.x + Lights[i].vLightAtt.y * dl + Lights[i].vLightAtt.z * dl * dl);

#if defined(CLEARCOAT)
            float Dc = Filament_D_GGX(clearCoatLinearRoughness, NdotH, N, H);
            float Vc = V_Kelemen(LdotH);
            float Fc = Filament_F_Schlick(0.04, LdotH) * clearCoat;
            float Frc = (Dc * Vc) * Fc;
            acc_color = mad(att, NdotL * Lights[i].vLightColor.rgb * ((diffuse + specular * (1 - Fc)) * (1 - Fc) + Frc), acc_color);
#else
            acc_color = mad(att, NdotL * Lights[i].vLightColor.rgb * (diffuse + specular), acc_color);
#endif
        }
        else if (Lights[i].iLightType == 3)
        {
            float3 L = (float3)(Lights[i].vLightPos - wp);
            float dl = length(L);
            if (Lights[i].vLightAtt.w < dl)
            {
                continue;
            }

            L = L / dl;
            float3 H = normalize(V + L);
            float3 sd = normalize((float3)Lights[i].vLightDir);
            const float NdotL = saturate(dot(N, L));
            const float LdotH = saturate(dot(L, H));
            const float NdotH = saturate(dot(N, H));
            float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
            float3 diffuse = c_diff * diffuse_factor;
            float3 specular = Specular_BRDF(alpha, c_spec, NdotV, NdotL, LdotH, NdotH, N, H);
            float rho = dot(-L, sd);
            float spot = pow(saturate((rho - Lights[i].vLightSpot.x) / (Lights[i].vLightSpot.y - Lights[i].vLightSpot.x)), Lights[i].vLightSpot.z);
            float att = spot / (Lights[i].vLightAtt.x + Lights[i].vLightAtt.y * dl + Lights[i].vLightAtt.z * dl * dl);

#if defined(CLEARCOAT)
            float Dc = Filament_D_GGX(clearCoatLinearRoughness, NdotH, N, H);
            float Vc = V_Kelemen(LdotH);
            float Fc = Filament_F_Schlick(0.04, LdotH) * clearCoat;
            float Frc = (Dc * Vc) * Fc;
            acc_color = mad(att, NdotL * Lights[i].vLightColor.rgb * ((diffuse + specular * (1 - Fc)) * (1 - Fc) + Frc), acc_color);
#else
            acc_color = mad(att, NdotL * Lights[i].vLightColor.rgb * (diffuse + specular), acc_color);
#endif
        }
    }

    if (bHasIrradianceMap)
    {
        float3 diffuse_env = Diffuse_IBL(N);
        acc_color += c_diff * diffuse_env;
    }

    float3 specular_env = vLightAmbient.rgb * ambientOcclusion;

#if defined(CLEARCOAT)
    float3 clearCoatColor = (float3)0;
    float Fc = Filament_F_Schlick(0.04, NdotV) * clearCoat;
#endif

    if (bHasCubeMap)
    {
        specular_env = Specular_IBL(N, V, roughness);

#if defined(CLEARCOAT)
        clearCoatColor = Specular_IBL(N, V, clearCoatRoughness) * Fc;
#endif
    }

    acc_color += c_spec * specular_env;

#if defined(CLEARCOAT)
    acc_color *= sqrt(1 - Fc);
    acc_color += clearCoatColor;
#endif

    return acc_color;
}


float4 EvaluateTriplanarPBR(PSInput input)
{
    const float3 V = normalize(input.vEye.xyz);

    float3 geometricNormal = CalculateGeometricNormal(input);
    TriplanarData triplanar = BuildTriplanarData(
        input.wp.xyz,
        geometricNormal);
    float3 N = CalculateShadingNormal(
        triplanar,
        geometricNormal);

    float4 albedo = float4(input.cDiffuse.xyz, 1.0f);
    float3 RMA = float3(
        ConstantAO,
        ConstantRoughness,
        ConstantMetallic);

    if (bHasDiffuseMap)
    {
        albedo *= SampleTriplanarAlbedo(triplanar);
    }

    albedo = lerp(albedo, input.c, vertColorBlending);

    if (bHasRMMap)
    {
        RMA.gb *= SampleTriplanarRM(triplanar).gb;
    }

    if (bHasAOMap)
    {
        RMA.r *= SampleTriplanarAO(triplanar);
    }
    else if (SSAOEnabled)
    {
        float2 quadTex = input.p.xy * vResolution.zw;
        RMA.r *= texSSAOMap.SampleLevel(
            samplerSurface,
            quadTex,
            0.0f).r;
    }

    float3 color = LightSurface(
        input.wp,
        V,
        N,
        albedo.rgb,
        RMA.g,
        RMA.b,
        RMA.r,
        ConstantReflectance,
        ClearCoat,
        ClearCoatRoughness);

    float shadow = 1.0f;
    if (bHasShadowMap && bRenderShadowMap)
    {
        float d = dot(getLookDir(vLightView), N);
        if (d > 0.0f)
        {
            shadow = shadowStrength(input.sp);
        }
    }

    color *= shadow;

    float3 emissive = vMaterialEmissive.rgb;
    if (bHasEmissiveMap)
    {
        emissive *= SampleTriplanarEmissive(triplanar);
    }

    float3 ambient = vLightAmbient.rgb * RMA.r;
    color += emissive + ambient;

    return float4(color, albedo.a * input.cDiffuse.a);
}

#endif
