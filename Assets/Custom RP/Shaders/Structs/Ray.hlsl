#ifndef DATA_STRUCT_RAY
#define DATA_STRUCT_RAY

struct Ray
{
    float3 origin;
    float3 direction;
    float3 invDirection;
    float3 energy;
};

Ray Make_Ray(float3 o, float3 d)
{
    d = normalize(d);
    Ray result;
    result.origin = o;
    result.direction = d;
    result.invDirection = 1.0 / d;
    result.energy = float3(1, 1, 1);
    return result;
}

float3 At(in Ray ray, in float t)
{
    return ray.origin + ray.direction * t;
}

#endif
