#ifndef DATA_STRUCT_RAY
#define DATA_STRUCT_RAY

struct Ray
{
    float3 origin;
    float3 direction;
    float3 invDirection;
    float tMin;
    float tMax;
};

static inline Ray Make_Ray( in float3 o, in float3 d, in float min, in float max)
{
    Ray result;
    result.origin = o;
    result.direction = d;
    result.invDirection = 1.0 / d;
    result.tMin = min;
    result.tMax = max;
    return result;
}

static inline float3 At(in Ray ray, in float t)
{
    return ray.origin + ray.direction * t;
}

#endif
