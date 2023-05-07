Shader "Teleport"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [HDR]_Colour("Colour", Color) = (1,1,1,1)
        [HDR]_EdgeColour("Colour", Color) = (1,1,1,1)
        _Speed("Speed", Float) = 50
    }

    //TODO https://gamedev.stackexchange.com/questions/131978/shader-reconstructing-position-from-depth-in-vr-through-projection-matrix/140924#140924
       SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionHCS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                //float3 cameraDir    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: The pass is setup with a mesh already in clip
                // space, that's why, it's enough to just output vertex
                // positions
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                    output.positionCS.y *= -1;
                #endif
                output.uv = input.uv;
                return output;
            }
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            uniform float4x4 _CameraToWorld;

            float4 _Colour;
            float4 _EdgeColour;
            float _Speed;

            uniform float _StartTime;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.uv);
                float depth = LinearEyeDepth(SampleSceneDepth(input.uv), _ZBufferParams);

                float time = max(0.0f, _Time.y - _StartTime) * _Speed;
                float t = saturate((time - depth) / 40.0f);
                return float4(lerp(lerp(_EdgeColour, _Colour, pow(t, 2.0)).rgb, color.rgb, saturate(t + pow(time / 500.0f, 2.0))), color.a);
            }
            ENDHLSL
        }
    }
}