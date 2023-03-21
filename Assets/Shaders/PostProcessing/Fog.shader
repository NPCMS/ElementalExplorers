Shader "Fog"
{
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
            #include "FogInclude.hlsl"
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
                float3 cameraDir    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4x4 _ViewProjectInverseLeft;
            float4x4 _ViewProjectInverseRight;

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
                float4x4 viewProject = input.uv.x > 1 ? _ViewProjectInverseRight : _ViewProjectInverseLeft;
                float2 uv = input.uv.x > 1 ? input.uv.x - 1 : input.uv.x;
                uv.y = input.uv.y;
                float4 cameraLocalDir = mul(viewProject, float4(uv.x * 2.0 - 1.0, uv.y * 2.0 - 1.0, 0.5, 1.0));
                cameraLocalDir.xyz /= cameraLocalDir.w;
                cameraLocalDir.xyz -= _WorldSpaceCameraPos;

                float4 cameraForwardDir = mul(viewProject, float4(0.0, 0.0, 0.5, 1.0));
                cameraForwardDir.xyz /= cameraForwardDir.w;
                cameraForwardDir.xyz -= _WorldSpaceCameraPos;

                output.cameraDir = cameraLocalDir.xyz / length(cameraForwardDir.xyz);
                output.uv = input.uv;
                return output;
            }

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            uniform float4x4 _CameraToWorld;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.uv);
                float depth = LinearEyeDepth(SampleSceneDepth(input.uv), _ZBufferParams);

                // fragment
                float3 viewSpaceViewDir = mul(unity_CameraInvProjection, float4(input.positionCS.xyz, 0));
                // unity_CameraInvProjection matches OpenGL projection matrix and will need to be flipped for other APIs
                #ifdef UNITY_REVERSED_Z
                                viewSpaceViewDir.y *= -1;
                #endif

                // don't use unity_CameraToWorld, it's not the same as the inverse of UNITY_MATRIX_V
                // however since the view matrix is a uniformly scaled matrix, the transpose is identical to the inverse
                // so use mul with the matrix and vector order swapped to get the view space to world space transform
                float3 worldSpaceViewDir = normalize(mul(viewSpaceViewDir, (float3x3)UNITY_MATRIX_V));

                float4 dir = float4(2.0 * input.uv - 1, 1, 0);
                //float3 viewDir = normalize(mul(unity_CameraToWorld, dir).xyz);
                float3 fog = applyFogWithMist(color, depth, normalize(input.cameraDir), _WorldSpaceCameraPos.y);
                return float4(fog, color.a);
            }
            ENDHLSL
        }
    }
}