#ifndef DATA_STRUCT_INTERSECTION
#define DATA_STRUCT_INTERSECTION

#include "Ray.hlsl"

struct HitRecord
{
    float3 p;
    float3 normal;
    uint materialIndex;
    float t;
    float frontFace;
};

static inline void SetFaceNormal(inout HitRecord record, in Ray ray, in float3 outwardNormal)
{
    record.frontFace = dot(ray.direction, outwardNormal) < 0;
    record.normal = record.frontFace ? outwardNormal : -outwardNormal;
}

#endif
