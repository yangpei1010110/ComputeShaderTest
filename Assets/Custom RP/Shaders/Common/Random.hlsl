#ifndef COMMON_RANDOM
#define COMMON_RANDOM

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

static inline float InterleavedGradientNoise(float2 xy)
{
    return frac(52.9829189f * frac(xy.x * 0.06711056f + xy.y * 0.00583715f));
}

static inline float RandomNextFloat(uint seed, float min, float max)
{
    return Hash(seed + _time.x * 1000) * (max - min) + min;
}

static inline float3 RandomFloat3(uint seed)
{
    return float3(Hash(seed + _time.x * 2000), Hash(seed + _time.x * 3000 + 1), Hash(seed + _time.x * 4000 + 2));
}

static inline float3 RandomFloat3(uint seed, float min, float max)
{
    return float3(RandomNextFloat(seed, min, max), RandomNextFloat(seed + 1, min, max), RandomNextFloat(seed + 2, min, max));
}

static inline float3 RandomInUnitSphere(uint seed)
{
    float3 p;
    do
    {
        p = RandomFloat3(seed, -1, 1);
        seed += 3;
    }
    while (length(p) >= 1);
    return p;
}

static inline float3 RandomInUnitHemisphere(uint seed, float3 normal)
{
    float3 p = RandomInUnitSphere(seed);
    return dot(p, normal) > 0 ? p : -p;
}

static inline float3 RandomUnitVector(uint seed)
{
    return normalize(RandomInUnitSphere(seed));
}

static inline float3 RandomInUnitDisk(uint seed)
{
    float3 p;
    do
    {
        p = float3(RandomNextFloat(seed, -1, 1), RandomNextFloat(seed + 1, -1, 1), 0);
    }
    while (dot(p, p) >= 1);
    return p;
}

#endif
