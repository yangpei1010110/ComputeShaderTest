#ifndef DATA_STRUCT_BVH_NODE
#define DATA_STRUCT_BVH_NODE

#include "Bounds.hlsl"

struct BvhNode
{
    Bounds Value;
    int GameObjectId;
    int triangleIndex;
};


static inline void Init(inout BvhNode result, in Bounds Value, in int GameObjectId, int triangleIndex)
{
    result.Value = Value;
    result.GameObjectId = GameObjectId;
    result.triangleIndex = triangleIndex;
}

#endif
