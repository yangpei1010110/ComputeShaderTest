#ifndef COMPUTE_INPUT
#define COMPUTE_INPUT

#include "../Structs/Bounds.hlsl"
#include "../Structs/Ray.hlsl"
#include "../Structs/BvhNode.hlsl"

RWStructuredBuffer<float3> Vertices;
RWStructuredBuffer<int> Triangles;
RWStructuredBuffer<BvhNode> BvhTree;
int BvhTreeCount;
int TrianglesCount;
RWTexture2D<float4> Result;
float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;

float3 lookFrom;
float3 lookAt;
float3 vUp;
float vFov;
float aspectRatio;
float aperture;
float focusDistance;

#endif
