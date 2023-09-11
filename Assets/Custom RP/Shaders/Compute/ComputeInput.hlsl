#ifndef COMPUTE_INPUT
#define COMPUTE_INPUT

#include "../Structs/Bounds.hlsl"
#include "../Structs/Ray.hlsl"
#include "../Structs/BvhNode.hlsl"

RWStructuredBuffer<BvhNode> tree;
RWTexture2D<float4> Result;

#endif