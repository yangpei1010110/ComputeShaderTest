using System;
using System.IO;
using UnityEngine;

namespace Simple1
{
    public class RayTracingRegister : MonoBehaviour
    {
        private void OnEnable()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                RayTracingMaster.RegisterMeshObject(meshFilter);
            }
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                RayTracingMaster.IsNeedRebuild = true;
                transform.hasChanged = false;
            }
        }

        private void OnDisable()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                RayTracingMaster.UnregisterMeshObject(meshFilter);
            }
        }
    }
}