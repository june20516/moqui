// 모기의 흐린 시야 (spec/11 §1): 플레이어 기준 거리로 안개색을 섞고, 흐림에 비례해 블러한다.
// URP FullScreenPassRendererFeature가 투명 렌더링 전에 실행한다 (CO₂·체온·은신처 표시는 안개 영향 없음).
// 전역 파라미터는 SensesFog.cs가 설정한다. _MoquiFogMax가 0이면 원본을 그대로 낸다.
Shader "Moqui/SensesFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.62, 0.66, 0.76, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "SensesFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _FogColor;
            float3 _MoquiFogOrigin;
            float _MoquiFogClear;
            float _MoquiFogFull;
            float _MoquiFogMax;
            float _MoquiFogBlur;

            static const int BlurTaps = 8;

            // FogModel.Amount와 같은 식.
            float FogAmount(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float span = max(_MoquiFogFull - _MoquiFogClear, 1e-4);
                return saturate((distance(world, _MoquiFogOrigin) - _MoquiFogClear) / span) * _MoquiFogMax;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_MoquiFogMax <= 0)
                {
                    return color;
                }

                float amount = FogAmount(uv);
                float blurFraction = amount / _MoquiFogMax;
                float2 radius = blurFraction * _MoquiFogBlur * (_ScreenParams.y / 1080.0) / _ScreenParams.xy;
                half3 blurred = color.rgb;
                [unroll]
                for (int i = 0; i < BlurTaps; i++)
                {
                    float angle = i * (TWO_PI / BlurTaps);
                    blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(cos(angle), sin(angle)) * radius).rgb;
                }

                blurred /= (BlurTaps + 1);
                return half4(lerp(blurred, _FogColor.rgb, amount), color.a);
            }
            ENDHLSL
        }
    }
}
