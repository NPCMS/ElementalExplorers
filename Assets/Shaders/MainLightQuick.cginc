
uniform float3 _SunColor;
uniform float3 _SunDirection;

void GetMainLight_float(out float3 Direction, out float3 Color)
{
#if SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
#else
    Direction = _SunDirection;
    Color = _SunColor;    
#endif
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