#ifndef DATA_STRUCT_BOUNDS
#define DATA_STRUCT_BOUNDS

struct Bounds
{
    float3 center;
    float3 extents;
};

Bounds Make_Bounds(in float3 c, in float3 e)
{
    Bounds result;
    result.center = c;
    result.extents = e;
    return result;
}

float3 GetSize(in Bounds bounds)
{
    return bounds.extents * 2;
}

void SetSize(inout Bounds bounds, in float3 size)
{
    bounds.extents = size * 0.5;
}

float3 GetMin(in Bounds bounds)
{
    return bounds.center - bounds.extents;
}

float3 GetMax(in Bounds bounds)
{
    return bounds.center + bounds.extents;
}

void SetMinMax(inout Bounds bounds, in float3 min, in float3 max)
{
    bounds.center = (min + max) * 0.5;
    bounds.extents = bounds.center - min;
}

void SetMax(inout Bounds bounds, in float3 max)
{
    SetMinMax(bounds, GetMin(bounds), max);
}

void SetMin(inout Bounds bounds, in float3 min)
{
    SetMinMax(bounds, min, GetMax(bounds));
}

bool Equals(in Bounds bounds, in Bounds other)
{
    return bounds.center == other.center && bounds.extents == other.extents;
}

#endif
