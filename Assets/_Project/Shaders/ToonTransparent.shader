// 툰 셰이더의 반투명 변형 (spec/10): 은신처 표시·유리·거미줄·체온 빛·물린 자국처럼 뒤가 비쳐야 하는 표시.
// 셀 2단 + 가장자리 림(빛나는 테두리)으로 같은 화풍을 유지한다. 외곽선은 그리지 않는다.
Shader "Moqui/ToonTransparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 0.5)
        _BaseMap ("Base Map", 2D) = "white" {}
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.6
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            Name "ToonTransparent"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _RimStrength;
                float _RimPower;
                float _Cull;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normal = normalize(input.normalWS) * (frontFace ? 1.0 : -1.0);
                float3 view = normalize(GetCameraPositionWS() - input.positionWS);
                Light light = GetMainLight();
                float lit = dot(normal, light.direction) > 0.0 ? 1.0 : 0.75;
                float rim = pow(1.0 - saturate(abs(dot(normal, view))), _RimPower) * _RimStrength;
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                return half4(texel.rgb * lit + rim * _BaseColor.rgb, saturate(texel.a + rim * _BaseColor.a));
            }
            ENDHLSL
        }
    }
}
