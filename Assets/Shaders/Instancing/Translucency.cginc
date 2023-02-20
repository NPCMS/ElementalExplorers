

void GetMainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float DistanceAtten, out float ShadowAtten)
{
    #if SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
    #else
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(WorldPos);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
    #endif
}

void CalculateTranslucency_float(float3 Tint, float3 LightDir, float3 LightCol, float ShadowAtten, float3 Normal, float3 ViewDirection, out float3 TransColor)
{
    float3 halfway = normalize(-LightDir + Normal * _TranslucencyDistortion);
    float amount = max(_Absorbsion, dot(halfway, ViewDirection));
    TransColor = pow(saturate(amount), _TranslucencyPower) * Tint  * ShadowAtten * LightCol;
}