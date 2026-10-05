// 공격 예고 표시 (spec/02 §7, M12 D-052): 화면에서 가장 높은 위상의 정보. 카메라를 향한 판에 그린다.
// 예고 진행률(_Progress)에 따라 바깥 고리가 판정 크기로 좁혀 들어오고, 안쪽이 차오르며, 진행될수록 빠르게 깜빡인다.
// 판정 중(_Strike = 1)에는 붉게 번쩍인다.
Shader "Moqui/TelegraphRing"
{
    Properties
    {
        _BaseColor ("Telegraph Color", Color) = (1, 0.55, 0.1, 1)
        _StrikeColor ("Strike Color", Color) = (1, 0.1, 0.1, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Strike ("Strike", Range(0, 1)) = 0
        _HitRadius ("Hit Radius (of quad half size)", Range(0.05, 1)) = 0.34
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.045
        _PulseSpeed ("Pulse Speed", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "TelegraphRing"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _StrikeColor;
                float _Progress;
                float _Strike;
                float _HitRadius;
                float _RingWidth;
                float _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Ring(float distance, float radius, float width)
            {
                return smoothstep(width, 0.0, abs(distance - radius));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float distance = length(input.uv * 2.0 - 1.0);
                float progress = saturate(_Progress);

                // 바깥 고리: 판 가장자리에서 판정 반경으로 좁혀 온다.
                float closing = lerp(0.98, _HitRadius, progress);
                float outer = Ring(distance, closing, _RingWidth);

                // 판정 반경 고리는 늘 보이고, 진행될수록 빠르게 깜빡인다.
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed * (1.0 + 3.0 * progress) * 6.2831853);
                float hitRing = Ring(distance, _HitRadius, _RingWidth * 0.8) * (0.55 + 0.45 * pulse);

                // 안쪽 채움: 진행률만큼 차오른다.
                float fill = step(distance, _HitRadius) * progress * 0.35;

                float telegraphAlpha = saturate(outer + hitRing + fill);
                float strikeAlpha = step(distance, _HitRadius * 1.15) * (0.75 + 0.25 * pulse);
                float alpha = lerp(telegraphAlpha, strikeAlpha, _Strike);
                float3 color = lerp(_BaseColor.rgb, _StrikeColor.rgb, _Strike);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
