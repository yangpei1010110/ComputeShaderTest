#ifndef COMPUTE_INPUT
#define COMPUTE_INPUT

#include "../Structs/Bounds.hlsl"
#include "../Structs/Ray.hlsl"
#include "../Structs/BvhNode.hlsl"

RWStructuredBuffer<float3> Vertices;
RWStructuredBuffer<BvhNode> BvhTree;
int BvhTreeCount;
RWTexture2D<float4> Result;
float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;

#endif
