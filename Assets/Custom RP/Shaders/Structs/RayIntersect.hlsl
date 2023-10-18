#ifndef DATA_STRUCT_RAY_INTERSECT
#define DATA_STRUCT_RAY_INTERSECT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "../Compute/ComputeInput.hlsl"
#include "RayHit.hlsl"
#include "../Common/VectorMath.hlsl"

// AABB 光线 求交算法
bool Intersects(Bounds bounds, Ray ray)
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
bool RayIntersectsTriangle(Ray ray, float3 t0, float3 t1, float3 t2, out float outIntersectionT)
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
bool Raycast(int treeIndex, Ray ray, out RayHit hit)
{
    struct StackData
    {
        int input_treeIndex;
        RayHit input_hit;
        bool result;

        BvhNode temp_node;
        bool temp_result1;
        RayHit temp_hit1;
        bool temp_result2;
        RayHit temp_hit2;

        int state_handle;
    };

    hit = Make_RayHit(0, FLT_MAX, 0);

    const int MAX_STACK_SIZE = 64;
    StackData stack[MAX_STACK_SIZE];
    int top = 0;
    stack[top].input_hit = hit;
    stack[top].input_treeIndex = 0;
    stack[top].temp_node = Make_BvhNode(Make_Bounds(0, 0), 0, 0, 0);
    stack[top].temp_result1 = 0;
    stack[top].temp_hit1 = Make_RayHit(0, FLT_MAX, 0);
    stack[top].temp_result2 = 0;
    stack[top].temp_hit2 = Make_RayHit(0, FLT_MAX, 0);
    stack[top].state_handle = 0;
    top++;
    UNITY_LOOP
    while (top > 0)
    {
        int current = top - 1;
        UNITY_BRANCH
        switch (stack[current].state_handle)
        {
        case 0:
            {
                UNITY_BRANCH
                if (stack[current].input_treeIndex >= BvhTreeCount)
                {
                    // out side of tree
                    stack[current].result = false;
                    top--;
                    break;
                }
                else
                {
                    // data init
                    stack[current].temp_node = BvhTree[stack[current].input_treeIndex];
                    // aabb 相交测试
                    UNITY_BRANCH
                    if (!Intersects(stack[current].temp_node.value, ray))
                    {
                        // 未命中 返回
                        stack[current].result = false;
                        top--;
                        break;
                    }
                    else
                    {
                        // 命中 继续
                        UNITY_BRANCH
                        if (stack[current].temp_node.gameObjectId != 0)
                        {
                            // 叶子节点 有对象
                            // intersect with triangle
                            float3 v0 = Vertices[Triangles[stack[current].temp_node.triangleIndex * 3 + 0]];
                            float3 v1 = Vertices[Triangles[stack[current].temp_node.triangleIndex * 3 + 1]];
                            float3 v2 = Vertices[Triangles[stack[current].temp_node.triangleIndex * 3 + 2]];
                            float oldDistance = stack[current].input_hit.distance;
                            float distance = oldDistance;
                            UNITY_BRANCH
                            if (RayIntersectsTriangle(ray, v0, v1, v2, distance))
                            {
                                stack[current].input_hit.position = ray.origin + ray.direction * distance;
                                stack[current].input_hit.distance = distance;
                                stack[current].result = true;
                            }
                            else
                            {
                                stack[current].input_hit.distance = oldDistance;
                                stack[current].result = false;
                            }
                            stack[current].input_hit.normal = normalize(cross(v0 - v1, v0 - v2));
                            top--;
                            break;
                        }
                        else
                        {
                            // is node
                            // intersect with aabb
                            stack[top].input_treeIndex = stack[current].input_treeIndex * 2 + 1;
                            stack[top].input_hit = Make_RayHit(0, FLT_MAX, 0);
                            stack[top].temp_node = Make_BvhNode(Make_Bounds(0, 0), 0, 0, 0);
                            stack[top].temp_result1 = 0;
                            stack[top].temp_hit1 = Make_RayHit(0, FLT_MAX, 0);
                            stack[top].temp_result2 = 0;
                            stack[top].temp_hit2 = Make_RayHit(0, FLT_MAX, 0);
                            stack[top].state_handle = 0;
                            top++;
                            stack[current].state_handle = 1;
                            break;
                        }
                    }
                }
            }

            break;
        case 1:
            stack[current].temp_result1 = stack[top].result;
            stack[current].temp_hit1 = stack[top].input_hit;
            stack[top].input_treeIndex = stack[current].input_treeIndex * 2 + 2;
            stack[top].input_hit = Make_RayHit(0, FLT_MAX, 0);
            stack[top].temp_node = Make_BvhNode(Make_Bounds(0, 0), 0, 0, 0);
            stack[top].temp_result1 = 0;
            stack[top].temp_hit1 = Make_RayHit(0, FLT_MAX, 0);
            stack[top].temp_result2 = 0;
            stack[top].temp_hit2 = Make_RayHit(0, FLT_MAX, 0);
            stack[top].state_handle = 0;
            top++;
            stack[current].state_handle = 2;
            break;
        case 2:
            stack[current].temp_result2 = stack[top].result;
            stack[current].temp_hit2 = stack[top].input_hit;
            stack[current].result = true;
            UNITY_BRANCH
            if (stack[current].temp_result1 && stack[current].temp_result2)
            {
                UNITY_BRANCH
                if (stack[current].temp_hit1.distance > stack[current].temp_hit2.distance)
                {
                    stack[current].input_hit = stack[current].temp_hit2;
                }
                else
                {
                    stack[current].input_hit = stack[current].temp_hit1;
                }
            }
            else if ((!stack[current].temp_result1) && stack[current].temp_result2)
            {
                stack[current].input_hit = stack[current].temp_hit2;
            }
            else if (stack[current].temp_result1 && (!stack[current].temp_result2))
            {
                stack[current].input_hit = stack[current].temp_hit1;
            }
            else
            {
                stack[current].result = false;
            }
            top--;
            break;
        default:
            break;
        }
    }
    hit = stack[0].input_hit;
    return stack[0].result;
}

float3 RayColor(Ray ray, int depth)
{
    struct StackData
    {
        Ray input_ray;
        int input_depth;
        float3 result;

        // RayHit temp_hit;
        float3 temp_attenuation;
        // Ray temp_scattered;

        int stateHandle;
    };
    const int MAX_STACK_SIZE = 64;
    StackData stack[MAX_STACK_SIZE];
    int top = 0;

    stack[top].input_ray = ray;
    stack[top].input_depth = depth;
    stack[top].result = 0;
    stack[top].temp_attenuation = 0;
    stack[top].stateHandle = 0;

    top++;
    int max = 0;
    UNITY_LOOP
    while (top > 0)
    {
        max++;
        UNITY_BRANCH
        if (max >= 256)
        {
            // 最大迭代次数
            return 0;
        }
        int current = top - 1;
        UNITY_LOOP
        switch (stack[current].stateHandle)
        {
        case 0:
            {
                RayHit hit = Create_RayHit();
                UNITY_BRANCH
                if (Raycast(0, stack[current].input_ray, hit))
                {
                    // stack[current].temp_hit = hit;
                    UNITY_BRANCH
                    if (stack[current].input_depth <= 0)
                    {
                        stack[current].result = 0;
                        top--;
                        break;
                    }
                    else
                    {
                        float3 attenuation = 0;
                        Ray scattered = Make_Ray(0, 0);
                        UNITY_BRANCH
                        if (DiffuseScatter(stack[current].input_ray, hit, attenuation, scattered))
                        {
                            stack[current].temp_attenuation = attenuation;
                            stack[top].input_ray = scattered;
                            stack[top].input_depth = stack[current].input_depth - 1;
                            stack[top].result = 0;
                            stack[top].temp_attenuation = 0;
                            stack[top].stateHandle = 0;
                            stack[current].stateHandle = 1;
                            top++;
                            break;
                        }
                        else
                        {
                            stack[current].result = 0;
                            top--;
                            break;
                        }
                    }
                }
                else
                {
                    float3 unitDirection = normalize(stack[current].input_ray.direction);
                    float t = 0.5f * (unitDirection.y + 1.0f);
                    stack[current].result = (1.0f - t) * float3(1, 1, 1) + t * float3(0.5, 0.7, 1.0);
                    top--;
                    break;

                    // float theta = acos(ray.direction.y) / -PI;
                    // float phi = atan2(ray.direction.x, -ray.direction.z) / -PI * 0.5;
                    // stack[current].result = _SkyboxTexture.SampleLevel(sampler_SkyboxTexture, float3(phi, theta, 0.0), 0.0);
                    // top--;
                    // break;
                }
            }
            break;
        case 1:
            {
                stack[current].result = stack[current].temp_attenuation * stack[top].result;
                top--;
            }
            break;
        default:
            break;
        }
    }
    return stack[0].result;
}

#endif
