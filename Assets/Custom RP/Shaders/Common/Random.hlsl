#ifndef COMMON_RANDOM
#define COMMON_RANDOM

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

float RandomNextFloat(uint seed, float min, float max)
{
    return Hash(seed) * (max - min) + min;
}

float3 RandomFloat3(uint seed)
{
    return float3(Hash(seed), Hash(seed + 1), Hash(seed + 2));
}

float3 RandomFloat3(uint seed, float min, float max)
{
    return float3(RandomNextFloat(seed, min, max), RandomNextFloat(seed + 1, min, max), RandomNextFloat(seed + 2, min, max));
}

float3 RandomInUnitSphere(uint seed)
{
    float3 p;
    do
    {
        p = RandomFloat3(seed, -1, 1);
    }
    while (length(p) >= 1);
    return p;
}

float3 RandomInUnitHemisphere(uint seed, float3 normal)
{
    float3 p = RandomInUnitSphere(seed);
    return dot(p, normal) > 0 ? p : -p;
}

float3 RandomUnitVector(uint seed)
{
    return normalize(RandomInUnitSphere(seed));
}

float3 RandomInUnitDisk(uint seed)
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
