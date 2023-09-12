#ifndef DATA_STRUCT_RAY_INTERSECT
#define DATA_STRUCT_RAY_INTERSECT

#include "Bounds.hlsl"
#include "Ray.hlsl"

// AABB 光线 求交算法
static inline bool Intersects(in Bounds bounds, in Ray ray, out float t)
{
    float tMin = 0, tMax = 1 / 0;
    float3 boxMin = GetMin(bounds);
    float3 boxMax = GetMax(bounds);
    float t1, t2;

    t1 = (boxMin.x - ray.origin.x) * ray.invDirection.x;
    t2 = (boxMax.x - ray.origin.x) * ray.invDirection.x;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    t1 = (boxMin.y - ray.origin.y) * ray.invDirection.y;
    t2 = (boxMax.y - ray.origin.y) * ray.invDirection.y;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    t1 = (boxMin.z - ray.origin.z) * ray.invDirection.z;
    t2 = (boxMax.z - ray.origin.z) * ray.invDirection.z;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    return tMin <= tMax;
}

// Möller–Trumbore 光线 三角形 求交算法
static inline bool RayIntersectsTriangle(in Ray ray, in float3 t0, in float3 t1, in float3 t2, out float3 outIntersectionPoint)
{
    const float EPSILON = 0.0000001;
    outIntersectionPoint = float3(0, 0, 0);
    float3 edge1, edge2, h, s, q;
    float a, f, u, v;
    edge1 = t1 - t0;
    edge2 = t2 - t0;
    h = cross(ray.direction, edge2);
    a = dot(edge1, h);
    if (a > -EPSILON && a < EPSILON)
    {
        return false;
    }

    f = 1.0 / a;
    s = ray.origin - t0;
    u = f * dot(s, h);

    if (u < 0.0 || u > 1.0)
    {
        return false;
    }

    q = cross(s, edge1);
    v = f * dot(ray.direction, q);

    if (v < 0.0 || u + v > 1.0)
    {
        return false;
    }

    float t = f * dot(edge2, q);
    if (t > EPSILON)
    {
        outIntersectionPoint = ray.origin + ray.direction * t;
        return true;
    }
    else
    {
        return false;
    }
}

#endif
