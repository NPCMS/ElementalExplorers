
uniform float3 _SunColor;
uniform float3 _SunDirection;

void GetMainLight_float(out float3 Direction, out float3 Color)
{
    Direction = _SunDirection;
    Color = _SunColor;
}

void GetShadows_float(float3 WorldPos, out float ShadowAtten)
{
    #if SHADERGRAPH_PREVIEW
    ShadowAtten = 1;
    #else
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(WorldPos);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif
    Light mainLight = GetMainLight(shadowCoord);
    ShadowAtten = mainLight.shadowAttenuation;
    #endif
}