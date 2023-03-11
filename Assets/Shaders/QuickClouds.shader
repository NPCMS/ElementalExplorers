Shader "Clouds/QuickClouds"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        [Normal]_Normal ("Normal Texture", 2D) = "white" {}
        _NormalScale ("Normal Scale", Float) = 1
        _PhaseFactor ("Phase Factor", Range(0,1)) = 1
        _PhaseFactor2 ("Phase Factor 2", Range(0,1)) = 1
        _Absorbance ("Absorbance", Float) = 0.1
        _AmbientColor("Ambient Colour", Color) = (1,1,1,1)
        _GroundColor("Ground Colour", Color) = (1,1,1,1)
        _DiffuseBleed("Diffuse Bleed", Range(0,1)) = 0.1 
        _WindAmount("Wind", Vector) = (0.01, 0.02, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalRenderPipeline" "Queue"="Transparent-200" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
                float3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
                float3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
                float3 viewDir : TEXCOORD4;
            };

            uniform half4 _LightColor0;

            Texture2D<float4> _MainTex;
            Texture2D<float4> _Normal;
            Texture2D<float4> _Mask;
            float4 _MainTex_ST;
            half _NormalScale;
            float _PhaseFactor;
            float _PhaseFactor2;
            float _DiffuseBleed;
            float _Absorbance;
            float3 _AmbientColor;
            float3 _GroundColor;
            float2 _WindAmount;

            SamplerState LinearRepeatSmp;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                half3 wNormal = TransformObjectToWorldNormal(v.normal);
                half3 wTangent = TransformObjectToWorldDir(v.tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                // output the tangent space matrix
                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                
                o.viewDir = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(v.vertex)));
                
                return o;
            }
            
            float hgPhase(float cosAngle, float phase)
            {
                float gSquared = phase * phase;
                return (0.079577471545948f) * (1 - gSquared) / pow(1 + gSquared - 2 * phase * cosAngle, 1.5f);
            }

            float4 frag (v2f i) : SV_Target
            {
                half2 cloudUV = i.uv + _Time.x * _WindAmount;
                float2 col = _MainTex.Sample(LinearRepeatSmp, cloudUV).ra;
                float3 nrm = UnpackNormalScale(_Normal.Sample(LinearRepeatSmp, cloudUV), _NormalScale);
                float mask = _Mask.Sample(LinearRepeatSmp, i.uv).r;
                float density = col.x * mask;
                float alpha = col.y * mask;
                float3 worldNormal = -float3(
                    dot(i.tspace0, nrm),
                    dot(i.tspace1, nrm),
                    dot(i.tspace2, nrm));
                
                float transmittance = exp(-density * _Absorbance);
                float phase = hgPhase(dot(i.viewDir, - _MainLightPosition), _PhaseFactor);
                phase += hgPhase(dot(i.viewDir, - _MainLightPosition), _PhaseFactor2);
                // float phase = hgPhase(dot(float3(i.tspace0.z, i.tspace1.z, i.tspace2.z), - _MainLightPosition));
                float groundAmount = saturate(dot(worldNormal, float3(0,-1,0)) + 0.25f);
                float3 lightEnergy = density * transmittance * phase * _MainLightColor;
                float nl = 1.0f - exp(2.0f * -(dot(-worldNormal, _MainLightPosition) + _DiffuseBleed));
                float3 color = lightEnergy + _AmbientColor * transmittance * (1 - groundAmount) + _GroundColor + _LightColor0 * saturate(nl);
                float a = saturate(1.0f - transmittance);
                return float4(color, a * alpha);
            }
            ENDHLSL
        }
    }
}
