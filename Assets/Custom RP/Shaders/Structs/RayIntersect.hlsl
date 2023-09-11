#ifndef DATA_STRUCT_RAY_INTERSECT
#define DATA_STRUCT_RAY_INTERSECT

#include "Bounds.hlsl"
#include "Ray.hlsl"


bool Intersects(in Bounds bounds, in Ray ray, out float t)
{
    float3 invDir = 1 / ray.direction;
    float3 t1 = (GetMin(bounds) - ray.origin) * invDir;
    float3 t2 = (GetMax(bounds) - ray.origin) * invDir;
    float3 tMin = min(t1, t2);
    float3 tMax = max(t1, t2);
    t = max(max(tMin.x, tMin.y), tMin.z);
    float tEnd = min(min(tMax.x, tMax.y), tMax.z);
    return t < tEnd && tEnd > 0;
}

#endif