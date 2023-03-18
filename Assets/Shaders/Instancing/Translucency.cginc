void CalculateTranslucency_float(float3 Tint, float3 LightDir, float3 LightCol, float ShadowAtten, float3 Normal, float3 ViewDirection, out float3 TransColor)
{
    float3 halfway = normalize(-LightDir + Normal * _TranslucencyDistortion);
    float amount = max(_Absorbsion, dot(halfway, ViewDirection));
    TransColor = pow(saturate(amount), _TranslucencyPower) * Tint  * ShadowAtten * LightCol;
}