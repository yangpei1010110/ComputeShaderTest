#ifndef COMPUTE_INPUT
#define COMPUTE_INPUT

#include "../Structs/Bounds.hlsl"
#include "../Structs/Ray.hlsl"
#include "../Structs/BvhNode.hlsl"

uniform Texture2D<float4> _SkyboxTexture;
uniform SamplerState sampler_SkyboxTexture;

uniform RWStructuredBuffer<float3> Vertices;
uniform RWStructuredBuffer<int> Triangles;
uniform RWStructuredBuffer<BvhNode> BvhTree;
uniform int BvhTreeCount;
uniform int TrianglesCount;
uniform RWTexture2D<float4> Result;
uniform float4x4 _CameraToWorld;
uniform float4x4 _CameraInverseProjection;

// float3 lookFrom;
// float3 lookAt;
// float3 vUp;
// float vFov;
// float aspectRatio;
// float aperture;
// float focusDistance;

#endif
