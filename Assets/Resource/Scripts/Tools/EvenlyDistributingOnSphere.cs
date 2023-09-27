#nullable enable
using Unity.Mathematics;
using UnityEngine;

namespace Resource.Scripts.Tools
{
    public static class EvenlyDistributingOnSphere
    {
        /// <summary>
        /// 球形斐波那契数列
        /// </summary>
        public static void Generation(in Vector3[] data)
        {
            int maxCount = data.Length;
            var phi = math.PI * (math.sqrt(5f) - 1f);
            for (int i = 0; i < maxCount; i++)
            {
                data[i].y = 1f - (i / (float)(maxCount - 1)) * 2f;
                float radius = math.sqrt(1f - data[i].y * data[i].y);
                float theta = phi * i;
                data[i].x = radius * math.cos(theta);
                data[i].z = radius * math.sin(theta);
            }
        }
    }
}