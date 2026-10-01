// 기체 표현용 절차적 노이즈 (텍스처 없이 셰이더에서 계산). SoftGas·VolumeFog가 함께 쓴다.
#ifndef MOQUI_GAS_NOISE_INCLUDED
#define MOQUI_GAS_NOISE_INCLUDED

float GasHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// 3D 값 노이즈 (0~1).
float GasValueNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(GasHash(i + float3(0, 0, 0)), GasHash(i + float3(1, 0, 0)), f.x),
             lerp(GasHash(i + float3(0, 1, 0)), GasHash(i + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(GasHash(i + float3(0, 0, 1)), GasHash(i + float3(1, 0, 1)), f.x),
             lerp(GasHash(i + float3(0, 1, 1)), GasHash(i + float3(1, 1, 1)), f.x), f.y),
        f.z);
}

// 3옥타브 fBm (0~1 근처).
float GasFbm(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 3; octave++)
    {
        value += amplitude * GasValueNoise(p);
        p = p * 2.03 + float3(17.1, 9.2, 3.7);
        amplitude *= 0.5;
    }

    return value / 0.875;
}

#endif
