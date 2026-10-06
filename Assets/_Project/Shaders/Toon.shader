// 프로젝트 공통 툰 셰이더 (spec/10): 메인 라이트 기준 셀 셰이딩(_Bands 단) + 남보라 그림자 색 + 월드 공간 외곽선.
// 방 분위기 조명·기믹 램프 같은 추가 조명(점·스포트)도 단계로 끊어 더한다 (spec/assets/lighting.md, M14).
// 출처가 다른 에셋도 베이스 컬러만 쓰고 이 셰이더로 통일한다. 렌더러별 색은 _BaseColor(MaterialPropertyBlock)로 준다.
Shader "Moqui/Toon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _ShadowTint ("Shadow Tint", Color) = (0.42, 0.38, 0.62, 1)
        _Bands ("Bands", Range(2, 4)) = 3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35
        _OutlineWidth ("Outline Width (world, max)", Float) = 0.6
        _OutlineDistanceRatio ("Outline Width per View Distance", Float) = 0.006
        _OutlineColor ("Outline Color", Color) = (0.12, 0.1, 0.2, 1)
        _SelfIllumination ("Self Illumination", Range(0, 1)) = 0
        _AdditionalBandSoftness ("Additional Light Band Softness", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ShadowTint;
            float _Bands;
            float _AmbientStrength;
            float _OutlineWidth;
            float _OutlineDistanceRatio;
            float4 _OutlineColor;
            float _SelfIllumination;
            float _AdditionalBandSoftness;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 추가 조명의 월드 위치 (URP 내부 저장 방식에 따라 읽는 곳이 다르다).
            float4 ToonAdditionalLightPosition(int perObjectIndex)
            {
            #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                return _AdditionalLightsBuffer[perObjectIndex].position;
            #else
                return _AdditionalLightsPosition[perObjectIndex];
            #endif
            }

            // 추가 조명 합: 게임 단위(1u ≈ 1cm)에서는 물리 감쇠(1/거리²)가 너무 빨리 꺼지므로 그 몫을 상쇄하고
            // 범위 창(가까우면 1, range에서 0)과 스포트 원뿔만 남긴다. 빛의 양은 _Bands 단으로 끊고 _AdditionalBandSoftness만큼 부드럽게 섞는다.
            float3 ToonAdditionalLights(float3 positionWS, float3 normal, float4 positionCS, float bands)
            {
                float3 sum = 0;
            #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                #if USE_CLUSTER_LIGHT_LOOP
                    int perObjectIndex = lightIndex;
                #else
                    int perObjectIndex = GetPerObjectLightIndex(lightIndex);
                #endif
                    float3 toLight = ToonAdditionalLightPosition(perObjectIndex).xyz - positionWS;
                    float window = saturate(light.distanceAttenuation * max(dot(toLight, toLight), HALF_MIN));
                    float amount = saturate(dot(normal, light.direction)) * window * light.shadowAttenuation;
                    float stepped = ceil(amount * bands - 0.15) / bands;
                    sum += light.color * lerp(saturate(stepped), amount, _AdditionalBandSoftness);
                LIGHT_LOOP_END
            #endif
                return sum;
            }

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

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float lit = saturate(dot(normal, light.direction)) * light.shadowAttenuation;

                // 셀 셰이딩: 빛의 양을 _Bands 단계로 끊는다.
                float bands = max(2.0, _Bands);
                float level = floor(lit * bands) / (bands - 1.0);
                float3 albedo = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor).rgb;
                float3 shade = lerp(_ShadowTint.rgb, 1.0, saturate(level));
                float3 ambient = SampleSH(normal) * _AmbientStrength;
                // 자체 밝기: 어두운 곳에서도 플레이어가 배경과 구분되도록 바탕색 쪽으로 끌어올린다 (spec/10 가독성).
                float3 additional = ToonAdditionalLights(input.positionWS, normal, input.positionCS, bands);
                float3 litColor = albedo * (shade * light.color + ambient + additional);
                return half4(lerp(litColor, albedo, _SelfIllumination), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            // 외곽선: 법선 방향으로 부풀린 뒷면을 그린다 (월드 공간 두께라 늘린 상자도 두께가 같다).
            // 두께는 시야 거리 × _OutlineDistanceRatio (화면에서 거의 일정한 굵기)이고 _OutlineWidth를 넘지 않는다.
            // 또 카메라를 향한 면은 카메라까지 거리(면 기준)의 절반보다 부풀리지 않는다: 카메라가 벽·천장에 바짝 붙어도
            // 외곽선 껍질 안에 들어가 그 뒷면이 화면을 덮는(까만 화면) 일이 없다. 실루엣 외곽선은 뒤를 향한 면에서 나오므로 그대로다 (M13).
            Name "ToonOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 toCamera = GetCameraPositionWS() - positionWS;
                float width = min(_OutlineWidth, _OutlineDistanceRatio * length(toCamera));
                float cameraInFront = dot(toCamera, normalWS);
                if (cameraInFront > 0.0)
                {
                    width = min(width, cameraInFront * 0.5);
                }

                return TransformWorldToHClip(positionWS + normalWS * width);
            }

            half4 Frag() : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
