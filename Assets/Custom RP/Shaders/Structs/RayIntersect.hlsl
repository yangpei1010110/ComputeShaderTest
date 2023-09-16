#ifndef DATA_STRUCT_RAY_INTERSECT
#define DATA_STRUCT_RAY_INTERSECT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "../Compute/ComputeInput.hlsl"
#include "RayHit.hlsl"


// AABB 光线 求交算法
static inline bool Intersects(in Bounds bounds, in Ray ray)
{
    float tMin = 0, tMax = FLT_MAX;
    float3 boxMin = GetMin(bounds);
    float3 boxMax = GetMax(bounds);
    float t1, t2;

    t1 = (boxMin.x - ray.origin.x) * ray.invDirection.x;
    t2 = (boxMax.x - ray.origin.x) * ray.invDirection.x;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    t1 = (boxMin.y - ray.origin.y) * ray.invDirection.y;
    t2 = (boxMax.y - ray.origin.y) * ray.invDirection.y;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    t1 = (boxMin.z - ray.origin.z) * ray.invDirection.z;
    t2 = (boxMax.z - ray.origin.z) * ray.invDirection.z;
    tMin = min(max(t1, tMin), max(t2, tMin));
    tMax = max(min(t1, tMax), min(t2, tMax));
    return tMin <= tMax;
}

// Möller–Trumbore 光线 三角形 求交算法
static inline bool RayIntersectsTriangle(in Ray ray, in float3 t0, in float3 t1, in float3 t2, out float outIntersectionT)
{
    const float EPSILON = 0.0000001;
    // outIntersectionT = 0;
    float3 edge1, edge2, h, s, q;
    float a, f, u, v;
    edge1 = t1 - t0;
    edge2 = t2 - t0;
    h = cross(ray.direction, edge2);
    a = dot(edge1, h);
    if (a > -EPSILON && a < EPSILON)
    {
        return false;
    }

    f = 1.0 / a;
    s = ray.origin - t0;
    u = f * dot(s, h);

    if (u < 0.0 || u > 1.0)
    {
        return false;
    }

    q = cross(s, edge1);
    v = f * dot(ray.direction, q);

    if (v < 0.0 || u + v > 1.0)
    {
        return false;
    }

    float t = f * dot(edge2, q);
    if (t > EPSILON)
    {
        outIntersectionT = t;
        return true;
    }
    else
    {
        return false;
    }
}

// TODO 实现 BVH 光线 求交算法
static inline bool Raycast(in int treeIndex, in Ray ray, out RayHit hit)
{
    struct StackData
    {
        int input_treeIndex;
        Ray input_ray;
        RayHit input_hit;

        bool result;

        int temp_index;
        int temp_left;
        int temp_right;
        BvhNode temp_node;
        bool temp_result1;
        RayHit temp_hit1;
        bool temp_result2;
        RayHit temp_hit2;

        int state_handle;
    };

    Init(hit, float3(0, 0, 0), 0, float3(0, 0, 0), 0);

    const int MAX_STACK_SIZE = 32;
    StackData stack[MAX_STACK_SIZE];
    int top = 0;
    stack[top].input_treeIndex = treeIndex;
    stack[top].input_ray = ray;
    stack[top].input_hit = hit;
    top++;
    int max = 0;
    while (top > 0)
    {
        max++;
        if (max >= 40)
        {
            return false;
        }
        int currentIndex = top - 1;
        StackData data = stack[currentIndex];
        switch (data.state_handle)
        {
        case 0:
            if (data.input_treeIndex >= BvhTreeCount)
            {
                data.result = false;
                stack[currentIndex] = data;
                top--;
                break;
            }

            data.temp_index = 0;
            data.temp_left = data.temp_index * 2 + 1;
            data.temp_right = data.temp_index * 2 + 2;
            data.temp_node = BvhTree[data.temp_index];
            if (data.temp_node.gameObjectId == 0)
            {
                // is leaf
                // intersect with triangle
                float3 v0 = Vertices[Triangles[data.temp_node.triangleIndex * 3 + 0]];
                float3 v1 = Vertices[Triangles[data.temp_node.triangleIndex * 3 + 1]];
                float3 v2 = Vertices[Triangles[data.temp_node.triangleIndex * 3 + 2]];
                data.input_hit.normal = cross(v0 - v1, v0 - v2);
                float oldDistance = data.input_hit.distance;
                float distance = oldDistance;
                if (RayIntersectsTriangle(ray, v0, v1, v2, distance))
                {
                    data.input_hit.distance = distance;
                    data.result = true;
                }
                else
                {
                    data.input_hit.distance = oldDistance;
                    data.result = false;
                }
                stack[currentIndex] = data;
                top--;
                break;
            }

        // is node
        // intersect with aabb
            stack[top].input_treeIndex = data.temp_left;
            stack[top].input_ray = data.input_ray;
            top++;
            data.state_handle = 1;
            stack[currentIndex] = data;
            break;
        case 1:
            data.temp_result1 = stack[top].result;
            data.temp_hit1 = stack[top].input_hit;
            stack[top].input_treeIndex = data.temp_right;
            stack[top].input_ray = data.input_ray;
            top++;
            data.state_handle = 2;
            stack[currentIndex] = data;
            break;
        case 2:
            data.temp_result2 = stack[top].result;
            data.temp_hit2 = stack[top].input_hit;
            if (data.temp_result1 && data.temp_result2)
            {
                data.result = true;
                if (data.temp_hit1.distance > data.temp_hit2.distance)
                {
                    data.input_hit = data.temp_hit2;
                }
                else
                {
                    data.input_hit = data.temp_hit1;
                }
            }
            else if (!data.temp_result1 && data.temp_result2)
            {
                data.result = true;
                data.input_hit = data.temp_hit2;
            }
            else if (data.temp_result1 && !data.temp_result2)
            {
                data.result = true;
                data.input_hit = data.temp_hit1;
            }
            else
            {
                data.result = false;
            }
            stack[currentIndex] = data;
            top--;
            break;
        default:
            break;
        }
    }
    hit = stack[0].input_hit;
    return stack[0].result;
}

#endif
