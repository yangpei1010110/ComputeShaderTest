#ifndef COMMON_RANDOM
#define COMMON_RANDOM

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "../Compute/ComputeInput.hlsl"

float3x3 GetTangentSpace(float3 normal)
{
    // Choose a helper vector for the cross product
    float3 helper = float3(1, 0, 0);
    if (abs(normal.x) > 0.99f)
        helper = float3(0, 0, 1);
    // Generate vectors
    float3 tangent = normalize(cross(normal, helper));
    float3 binormal = normalize(cross(normal, tangent));
    return float3x3(tangent, binormal, normal);
}

float InterleavedGradientNoise(float2 xy)
{
    return frac(52.9829189f * frac(xy.x * 0.06711056f + xy.y * 0.00583715f));
}

float rand()
{
    float result = frac(sin(_seed / 100.0f * dot(float2(0.5, 0.5), float2(12.9898f, 78.233f))) * 43758.5453f);
    _seed += 1.0f;
    return result;
}

float3 SampleHemisphere(float3 normal)
{
    // Uniformly sample hemisphere direction
    float cosTheta = rand();
    float sinTheta = sqrt(max(0.0f, 1.0f - cosTheta * cosTheta));
    float phi = 2 * PI * rand();
    float3 tangentSpaceDir = float3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
    // Transform direction to world space
    return mul(tangentSpaceDir, GetTangentSpace(normal));
}

float RandomNextFloat(uint seed, float min, float max)
{
    // return Hash(seed) * (max - min) + min;
    return Hash(seed * _seed * _frameCount) * (max - min) + min;
}

float3 RandomFloat3(uint seed)
{
    return float3(Hash(seed * _seed * _frameCount), Hash((seed + 1) * _seed * _frameCount), Hash((seed + 2) * _seed * _frameCount));
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
        seed += 3;
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
