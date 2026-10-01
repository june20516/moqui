// 증기 (spec/05 습기 영역): 박스 볼륨 안을 레이 마칭해 흐르는 3D 노이즈 밀도를 누적하는 볼륨 안개.
// 앞면 대신 뒷면을 그리고(Cull Front) 깊이 검사를 끄되, 장면 깊이에서 광선을 잘라 가구 뒤를 가리지 않는다.
// 그래서 카메라가 볼륨 안에 있어도 보인다. 박스 가장자리로 갈수록 밀도를 줄여 상자 모양이 드러나지 않게 한다.
Shader "Moqui/VolumeFog"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.9, 0.95, 1, 0.85)
        _Density ("Density (per unit)", Float) = 0.035
        _DensityScale ("Density Scale", Float) = 1
        _NoiseScale ("Noise Scale (per unit)", Float) = 0.06
        _NoiseSpeed ("Noise Speed", Float) = 0.25
        _EdgeSoftness ("Edge Softness", Float) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Front

        Pass
        {
            Name "VolumeFog"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "GasNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Density;
                float _DensityScale;
                float _NoiseScale;
                float _NoiseSpeed;
                float _EdgeSoftness;
            CBUFFER_END

            static const int MarchSteps = 24;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            // 단위 박스 [-0.5, 0.5]³와 광선(오브젝트 공간, 방향은 정규화하지 않음)의 교차 구간.
            float2 IntersectUnitBox(float3 origin, float3 direction)
            {
                float3 inverse = 1.0 / direction;
                float3 t0 = (-0.5 - origin) * inverse;
                float3 t1 = (0.5 - origin) * inverse;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                return float2(max(max(tMin.x, tMin.y), tMin.z), min(min(tMax.x, tMax.y), tMax.z));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 cameraWS = GetCameraPositionWS();
                float3 rayWS = normalize(input.positionWS - cameraWS);

                // 오브젝트 공간 방향을 정규화하지 않으면 교차 매개변수 t가 그대로 월드 거리가 된다.
                float3 originOS = TransformWorldToObject(cameraWS);
                float3 directionOS = mul((float3x3)GetWorldToObjectMatrix(), rayWS);
                float2 hit = IntersectUnitBox(originOS, directionOS);
                float tNear = max(hit.x, 0.0);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
                float tScene = sceneEye / max(dot(rayWS, cameraForward), 1e-3);
                float tFar = min(hit.y, tScene);
                if (tFar <= tNear)
                {
                    return 0;
                }

                float stepLength = (tFar - tNear) / MarchSteps;
                float time = _Time.y * _NoiseSpeed;
                float transmittance = 1.0;
                [loop]
                for (int i = 0; i < MarchSteps; i++)
                {
                    float t = tNear + (i + 0.5) * stepLength;
                    float3 positionWS = cameraWS + rayWS * t;
                    float3 positionOS = originOS + directionOS * t;

                    float3 toFace = 0.5 - abs(positionOS);
                    float edge = saturate(min(min(toFace.x, toFace.y), toFace.z) / max(_EdgeSoftness * 0.5, 1e-3));
                    float noise = GasFbm(positionWS * _NoiseScale + float3(0.0, time, time * 0.6));
                    // 가장자리로 갈수록 노이즈가 깎아 먹어 상자 윤곽 대신 뭉게뭉게한 경계가 된다.
                    float density = _Density * _DensityScale * saturate(noise * 1.6 - 0.25 - (1.0 - edge) * 0.9);
                    transmittance *= exp(-density * stepLength);
                }

                return half4(_BaseColor.rgb, (1.0 - transmittance) * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
