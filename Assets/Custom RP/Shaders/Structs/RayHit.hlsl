#ifndef DATA_STRUCT_RAYHIT
#define DATA_STRUCT_RAYHIT

struct RayHit
{
    float3 position;
    float distance;
    float3 normal;
    int materialId;
};

static inline RayHit Make_RayHit( in float3 position, in float distance, in float3 normal, in int materialId)
{
    RayHit result;
    result.position = position;
    result.distance = distance;
    result.normal = normal;
    result.materialId = materialId;
    return result;
}

#endif