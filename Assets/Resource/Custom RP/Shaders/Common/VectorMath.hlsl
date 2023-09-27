#ifndef COMMON_VECTORMATH
#define COMMON_VECTORMATH

#include "../Compute/ComputeInput.hlsl"
#include "../Structs//Ray.hlsl"
#include "../Structs//RayHit.hlsl"
#include "Random.hlsl"

bool NearZero(float3 v)
{
    const float s = 1e-8;
    return (abs(v.x) < s) && (abs(v.y) < s) && (abs(v.z) < s);
}

// 重心插值
float3 BarycentricInterpolation(float3 a, float3 b, float3 c, float3 p)
{
    float alpha = (-(p.x - b.x) * (c.y - b.y) + (p.y - b.y) * (c.x - b.x)) / (-(a.x - b.x) * (c.y - b.y) + (a.y - b.y) * (c.x - b.x));
    float beta = (-(p.x - c.x) * (a.y - c.y) + (p.y - c.y) * (a.x - c.x)) / (-(b.x - c.x) * (a.y - c.y) + (b.y - c.y) * (a.x - c.x));
    float gamma = 1.0 - alpha - beta;
    return float3(alpha, beta, gamma);
}

float3 Reflect(float3 v, float3 n)
{
    return v - 2 * dot(v, n) * n;
}

float3 Refract(float3 v, float3 n, float eta)
{
    float cosTheta = dot(v, n);
    float3 rOutParallel = eta * (v - cosTheta * n);
    float3 rOutPerp = -sqrt(1.0 - dot(rOutParallel, rOutParallel)) * n;
    return rOutParallel + rOutPerp;
}

float3 Schlick(float3 v, float3 n, float eta)
{
    float cosTheta = dot(v, n);
    float r0 = (1.0 - eta) / (1.0 + eta);
    r0 = r0 * r0;
    return r0 + (1.0 - r0) * pow(1.0 - cosTheta, 5.0);
}

float3 Fresnel(float3 v, float3 n, float eta)
{
    float cosTheta = dot(v, n);
    float r0 = (1.0 - eta) / (1.0 + eta);
    r0 = r0 * r0;
    return r0 + (1.0 - r0) * pow(1.0 - cosTheta, 5.0);
}

bool DiffuseScatter(Ray rIn, RayHit rec, out float3 attenuation, out Ray scattered)
{
    // float3 scatterDirection = rec.normal + RandomInUnitSphere((uint)(rec.position.x + rec.position.y * 10 + rec.position.z * 100));
    // scatterDirection = NearZero(scatterDirection) ? rec.normal : scatterDirection;
    float3 scatterDirection = SampleHemisphere(rec.normal);
    scattered = Make_Ray(rec.position, scatterDirection);
    attenuation = _debugDiffuseAlbedo;
    return true;
}


#endif
