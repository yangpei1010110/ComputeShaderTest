#ifndef DATA_STRUCT_RAY_INTERSECT
#define DATA_STRUCT_RAY_INTERSECT

#include "../Compute/ComputeInput.hlsl"


// AABB 光线 求交算法
static inline bool Intersects(in Bounds bounds, in Ray ray)
{
    float tMin = 0, tMax = 1 / 0;
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
static inline bool Raycast(in int treeIndex, in Ray ray, out float outIntersectionT)
{
    if (treeIndex >= BvhTreeCount)
    {
        return false;
    }
    outIntersectionT = 0;

    const int MAX_STACK_SIZE = 32;
    int stack[MAX_STACK_SIZE]; // max depth
    int treeIndexStack[MAX_STACK_SIZE];
    int tStack[MAX_STACK_SIZE];
    int index = 0;
    do
    {
        int left = index * 2 + 1;
        int right = index * 2 + 2;
        BvhNode node = BvhTree[index];
        if (node.gameObjectId == 0)
        {
            // is leaf
            // intersect with triangle
            float3 v0 = Vertices[Triangles[node.triangleIndex * 3 + 0]];
            float3 v1 = Vertices[Triangles[node.triangleIndex * 3 + 1]];
            float3 v2 = Vertices[Triangles[node.triangleIndex * 3 + 2]];
            float oldIntersectionT = outIntersectionT;
            if (RayIntersectsTriangle(ray, v0, v1, v2, outIntersectionT))
            {
                return true;
            }
            else
            {
                outIntersectionT = oldIntersectionT;
                return false;
            }
        }
        else
        {
            // is node
            // intersect with aabb
            float oldIntersectionT = outIntersectionT;
            if (Intersects(node.value, ray))
            {
                // push right
                stack[index] = 1;
                treeIndexStack[index] = right;
                tStack[index] = outIntersectionT;
                index++;
                // push left
                stack[index] = 0;
                treeIndexStack[index] = left;
                tStack[index] = outIntersectionT;
                index++;
            }
            else
            {
                return false;
            }
        }
    }
    while (true);

    return false;
}

#endif
