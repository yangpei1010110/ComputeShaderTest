#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Simple1
{
    public class RayTracingMaster : MonoBehaviour
    {
        public  Texture2D?     SkyboxTexture;
        public  ComputeShader? RayTracingShader;
        private RenderTexture? _target;
        private RenderTexture? _finalTarget;
        private Camera?        _camera;

        private uint      _currentSample = 0;
        private Material? _addMaterial;

        private static          int RayTracingComputeKernel  = -1;
        private static readonly int _CameraToWorld           = Shader.PropertyToID("_CameraToWorld");
        private static readonly int _CameraInverseProjection = Shader.PropertyToID("_CameraInverseProjection");
        private static readonly int Result                   = Shader.PropertyToID("Result");
        private static readonly int _SkyboxTexture           = Shader.PropertyToID("_SkyboxTexture");
        private static readonly int _PixelOffset             = Shader.PropertyToID("_PixelOffset");
        private static readonly int _Sample                  = Shader.PropertyToID("_Sample");
        private static readonly int _SphereBuffer            = Shader.PropertyToID("_SphereBuffer");
        private static readonly int _SphereBufferCount       = Shader.PropertyToID("_SphereBufferCount");


        private static readonly Dictionary<SphereCollider, float4> _sphereData      = new Dictionary<SphereCollider, float4>();
        private static          float4[]                           _sphereDataArray = Array.Empty<float4>();
        private static          ComputeBuffer?                     _SphereComputeBuffer;

        void Start()
        {
            _camera ??= GetComponent<Camera>();
            if (RayTracingShader != null)
            {
                RayTracingComputeKernel = RayTracingShader.FindKernel("RayTracingCompute");
                RenderPipelineManager.endCameraRendering += Render;
            }

            foreach (var go in gameObject.scene.GetRootGameObjects())
            {
                var sc = go.GetComponent<SphereCollider>();
                if (sc != null)
                {
                    float4 sphereData = new float4();
                    sphereData.xyz = go.transform.position;
                    sphereData.w = sc.radius;
                    _sphereData.Add(sc, sphereData);
                }
            }
        }

        private void Update()
        {
            bool isChange = false;
            foreach (var k in _sphereData.Keys.ToArray())
            {
                var newSphereData = new float4();
                newSphereData.xyz = k.transform.position;
                newSphereData.w = k.radius;

                if (math.any(newSphereData - _sphereData[k]))
                {
                    isChange = true;
                    _sphereData[k] = newSphereData;
                }
            }

            if (transform.hasChanged || isChange)
            {
                {
                    Array.Resize(ref _sphereDataArray, _sphereData.Count);
                    int i = 0;
                    foreach (var v in _sphereData.Values)
                    {
                        _sphereDataArray[i] = v;
                        i++;
                    }

                    _SphereComputeBuffer = new ComputeBuffer(_sphereData.Count, UnsafeUtility.SizeOf<float4>());
                    _SphereComputeBuffer.SetData(_sphereDataArray);
                }

                if (_finalTarget != null)
                {
                    _finalTarget.Release();
                }

                _currentSample = 0;
                transform.hasChanged = false;
            }
        }

        private void Render(ScriptableRenderContext src, Camera c)
        {
            if (RayTracingShader == null)
            {
                return;
            }

            if (c != _camera)
            {
                return;
            }

            InitRenderTexture();
            SetShaderParameters(c);
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            RayTracingShader.Dispatch(RayTracingComputeKernel, threadGroupsX, threadGroupsY, 1);

            if (_addMaterial == null)
            {
                _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            }

            _addMaterial.SetFloat(_Sample, _currentSample);
            Graphics.Blit(_target, _finalTarget, _addMaterial);
            Graphics.Blit(_finalTarget, c.targetTexture);
            _currentSample++;
        }

        private void SetShaderParameters(Camera c)
        {
            if (RayTracingShader == null)
            {
                return;
            }

            RayTracingShader.SetTexture(RayTracingComputeKernel, _SkyboxTexture, SkyboxTexture);
            RayTracingShader.SetTexture(RayTracingComputeKernel, Result, _target);

            RayTracingShader.SetMatrix(_CameraToWorld, c.cameraToWorldMatrix);
            RayTracingShader.SetMatrix(_CameraInverseProjection, c.projectionMatrix.inverse);

            RayTracingShader.SetVector(_PixelOffset, new Vector2(Random.value, Random.value));

            RayTracingShader.SetBuffer(RayTracingComputeKernel, _SphereBuffer, _SphereComputeBuffer);
            RayTracingShader.SetInt(_SphereBufferCount, _sphereDataArray.Length);
        }

        private void InitRenderTexture()
        {
            if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
            {
                if (_target != null)
                {
                    _target.Release();
                }
                else
                {
                    _target = new RenderTexture(Screen.width, Screen.height, 0,
                                                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                    _target.enableRandomWrite = true;
                    _target.Create();
                }
            }

            if (_finalTarget == null || _finalTarget.width != Screen.width || _finalTarget.height != Screen.height)
            {
                if (_finalTarget == null)
                {
                    _finalTarget = new RenderTexture(Screen.width, Screen.height, 0,
                                                     RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                    _finalTarget.enableRandomWrite = true;
                    _finalTarget.Create();
                }
                else
                {
                    _finalTarget.Release();
                }
            }
        }
    }
}