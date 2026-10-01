// CO₂ 연기 덩이 (spec/11 §2): 카메라를 향하는 쿼드에 방사형 감쇠와 흐르는 노이즈를 입힌 소프트 파티클.
// 장면 깊이와 가까운 곳은 서서히 투명해져(소프트 파티클) 가구에 닿아도 잘린 선이 보이지 않는다.
// 투명 큐라서 흐린 시야 패스 뒤에 그려지고 안개 영향을 받지 않는다. 덩이마다 진하기는 _BaseColor 알파로 준다.
Shader "Moqui/SoftGas"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.95, 0.78, 0.98, 0.5)
        _SoftDistance ("Soft Distance", Float) = 4
        _NoiseScale ("Noise Scale", Float) = 2.5
        _NoiseSpeed ("Noise Speed", Float) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SoftGas"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "GasNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _SoftDistance;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float seed : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                // 오브젝트 중심과 크기만 쓰고 방향은 카메라 평면에 맞춘다 (빌보드).
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float scale = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = center + ((right * input.positionOS.x) + (up * input.positionOS.y)) * scale;

                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.seed = frac(dot(center, float3(0.131, 0.713, 0.371)));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radial = saturate(1.0 - length(centered));
                radial = radial * radial * (3.0 - 2.0 * radial);

                float time = _Time.y * _NoiseSpeed;
                float3 noisePosition = float3(centered * _NoiseScale, input.seed * 10.0) + float3(0.0, -time, time * 0.5);
                float wisps = saturate(GasFbm(noisePosition) * 1.6 - 0.25);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float soft = saturate((sceneEye - input.screenPos.w) / max(_SoftDistance, 1e-3));

                float alpha = _BaseColor.a * radial * wisps * soft;
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
