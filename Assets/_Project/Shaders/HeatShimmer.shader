// 체온 표시 (spec/11 §3, M12 D-052): 부피 있는 막이 아니라 피부 윤곽을 따라 흐르는 얇은 림과 위로 일렁이는 아지랑이 선.
// 가산 혼합이라 어두운 곳에서 눈에 띄지만 피부를 덮지 않는다. 세기는 _BaseColor 알파(거리별, SensesView)로 준다.
Shader "Moqui/HeatShimmer"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.55, 0.45, 1)
        _RimPower ("Rim Power", Range(1, 8)) = 3.5
        _RimStrength ("Rim Strength", Range(0, 3)) = 1.4
        _BandFrequency ("Band Frequency (per u)", Float) = 0.45
        _BandSpeed ("Band Speed", Float) = 2.0
        _BandSharpness ("Band Sharpness", Range(1, 16)) = 6
        _FillAlpha ("Fill Alpha", Range(0, 0.2)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "HeatShimmer"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _RimPower;
                float _RimStrength;
                float _BandFrequency;
                float _BandSpeed;
                float _BandSharpness;
                float _FillAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetCameraPositionWS() - input.positionWS);
                float rim = pow(1.0 - saturate(abs(dot(normal, view))), _RimPower);

                // 위로 흐르는 가는 물결 선 (열기 아지랑이). 옆 방향 위치로 살짝 휘게 한다.
                float wave = input.positionWS.y * _BandFrequency - _Time.y * _BandSpeed + sin((input.positionWS.x + input.positionWS.z) * 0.3) * 0.6;
                float band = pow(0.5 + 0.5 * sin(wave * 6.2831853), _BandSharpness);

                float alpha = saturate((rim * _RimStrength * (0.55 + 0.45 * band)) + (band * rim * 0.5) + _FillAlpha) * _BaseColor.a;
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
