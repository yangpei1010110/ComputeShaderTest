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

static inline void Init(inout Ray result, in float3 o, in float3 d, in float min, in float max)
{
    result.origin = o;
    result.direction = d;
    result.invDirection = 1 / d;
    result.tMin = min;
    result.tMax = max;
}

static inline float3 At(in Ray ray, in float t)
{
    return ray.origin + ray.direction * t;
}

#endif
