float GetMainLight_float(out float3 LightColour, out float3 LightDir)
{
    LightColour = _LightColor0;
    LightDir = _MainLightPosition;
}

float hgPhase_float(float cosAngle, float phase)
{
    float gSquared = phase * phase;
    return (0.079577471545948f) * (1 - gSquared) / pow(1 + gSquared - 2 * phase * cosAngle, 1.5f);
}

//float4 frag(v2f i) : SV_Target
//{
//    UNITY_SETUP_INSTANCE_ID(i);
//    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
//    half2 cloudUV = i.uv + _Time.x * _WindAmount;
//    float2 col = _MainTex.Sample(LinearRepeatSmp, cloudUV).ra;
//    float3 nrm = UnpackNormalScale(_Normal.Sample(LinearRepeatSmp, cloudUV), _NormalScale);
//    float mask = _Mask.Sample(LinearRepeatSmp, i.uv).r;
//    float density = col.x * mask;
//    float alpha = col.y * mask;
//    float3 worldNormal = -float3(
//        dot(i.tspace0, nrm),
//        dot(i.tspace1, nrm),
//        dot(i.tspace2, nrm));
//                
//    float transmittance = exp(-density * _Absorbance);
//    float phase = hgPhase(dot(worldNormal, - _MainLightPosition), _PhaseFactor);
//    phase += hgPhase(dot(worldNormal, -_MainLightPosition), _PhaseFactor2);
//    // float phase = hgPhase(dot(float3(i.tspace0.z, i.tspace1.z, i.tspace2.z), - _MainLightPosition));
//    float groundAmount = saturate(dot(worldNormal, float3(0,-1,0)) + 0.25f);
//    float3 lightEnergy = density * transmittance * phase * _MainLightColor;
//    float nl = 1.0f - exp(2.0f * -(dot(-worldNormal, _MainLightPosition) + _DiffuseBleed));
//    float3 color = lightEnergy + _AmbientColor * transmittance * (1 - groundAmount) + _GroundColor + _LightColor0 * saturate(nl);
//    float a = saturate(1.0f - transmittance);
//    return float4(color, a * alpha);
//}