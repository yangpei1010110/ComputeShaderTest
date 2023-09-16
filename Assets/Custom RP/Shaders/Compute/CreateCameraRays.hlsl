#ifndef COMPUTE_CREATECAMERARAYS
#define COMPUTE_CREATECAMERARAYS

#include "../Compute/ComputeInput.hlsl"

Ray CreateCameraRays(float2 uv)
{
    // Transform the camera origin to world space
    float3 origin = mul(_CameraToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

    // Invert the perspective projection of the view-space position
    float3 direction = mul(_CameraInverseProjection, float4(uv, 1.0f, 1.0f)).xyz;
    // Transform the direction from camera to world space and normalize
    direction = mul(_CameraToWorld, float4(direction, 0.0f)).xyz;
    direction = normalize(direction);
    Ray ray;
    Init(ray, origin, direction, 0.001f, 1000.0f);
    return ray;
}

#endif
