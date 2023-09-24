using UnityEngine;

namespace Tools.Material
{
    public struct Diffuse
    {
        public Vector3 Albedo { get; set; }

        public Diffuse(bool random = false)
        {
            if (random)
            {
                Albedo = new Vector3(Random.value * Random.value,
                                     Random.value * Random.value,
                                     Random.value * Random.value);
            }
            else
            {
                Albedo = Vector3.one;
            }
        }
    }
}