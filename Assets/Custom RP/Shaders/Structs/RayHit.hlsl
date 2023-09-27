#ifndef DATA_STRUCT_RAYHIT
#define DATA_STRUCT_RAYHIT

struct RayHit
{
    float3 position;
    float distance;
    float3 normal;
};

RayHit Create_RayHit()
{
    RayHit result;
    result.position = float3(0, 0, 0);
    result.distance = 1.#INF;
    result.normal = float3(0, 0, 0);
    return result;
}

RayHit Make_RayHit(float3 position, float distance, float3 normal)
{
    RayHit result;
    result.position = position;
    result.distance = distance;
    result.normal = normal;
    return result;
}

#endif
