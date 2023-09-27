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
uniform RWTexture2D<float4> RealResult;
uniform float4x4 _CameraToWorld;
uniform float4x4 _CameraInverseProjection;
uniform float3 _debugDiffuseAlbedo;
uniform float4 _time;
uniform int _frameCount;
uniform float _seed;

#endif
