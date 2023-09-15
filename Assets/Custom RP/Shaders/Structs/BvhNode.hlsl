#ifndef DATA_STRUCT_BVH_NODE
#define DATA_STRUCT_BVH_NODE

#include "Bounds.hlsl"

struct BvhNode
{
    Bounds value;
    int gameObjectId;
    int materialId;
    int triangleIndex;
};


static inline void Init(inout BvhNode result,
                        in Bounds value,
                        in int gameObjectId,
                        in int materialId,
                        in int triangleIndex)
{
    result.value = value;
    result.gameObjectId = gameObjectId;
    result.materialId = materialId;
    result.triangleIndex = triangleIndex;
}

#endif
