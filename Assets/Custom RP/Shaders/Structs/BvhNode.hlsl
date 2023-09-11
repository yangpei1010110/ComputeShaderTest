#ifndef DATA_STRUCT_BVH_NODE
#define DATA_STRUCT_BVH_NODE

#include "Bounds.hlsl"

struct BvhNode
{
    Bounds Value;
    int GameObjectId;
    int triangleIndex;
};

#endif
