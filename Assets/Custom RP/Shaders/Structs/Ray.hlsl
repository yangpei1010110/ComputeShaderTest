#ifndef DATA_STRUCT_RAY
#define DATA_STRUCT_RAY

struct Ray
{
    float3 origin;
    float3 direction;
    float t;
    float tMin;
    float tMax;
};

float3 GetPoint(in Ray ray, in float t)
{
    return ray.origin + ray.direction * t;
}

#endif
