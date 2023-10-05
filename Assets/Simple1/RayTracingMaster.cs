#nullable enable
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Simple1
{
    public class RayTracingMaster : MonoBehaviour
    {
        public struct Sphere
        {
            public float3 position;
            public float  radius;
            public float3 albedo;
            public float3 specular;
            public float  smoothness;
            public float3 emission;
        };

        private static readonly int            _NewSphereBuffer = Shader.PropertyToID("_NewSphereBuffer");
        private static          ComputeBuffer? _NewSphereComputeBuffer;
        public                  int            MaxCount = 500;
        public                  float          MaxRange = 30f;
        public                  float2         MaxSize  = new float2(0.5f, 3f);

        public Light? DirectionalLight;

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
        private static readonly int _Seed                    = Shader.PropertyToID("_Seed");
        // private static readonly int _SphereBuffer            = Shader.PropertyToID("_SphereBuffer");
        // private static readonly int _SphereBufferCount       = Shader.PropertyToID("_SphereBufferCount");


        // private static readonly Dictionary<SphereCollider, float4> _sphereData      = new Dictionary<SphereCollider, float4>();
        // private static          float4[]                           _sphereDataArray = Array.Empty<float4>();
        // private static          ComputeBuffer?                     _SphereComputeBuffer;

        void Start()
        {
            _camera ??= GetComponent<Camera>();

            // foreach (var go in gameObject.scene.GetRootGameObjects())
            // {
            //     var sc = go.GetComponent<SphereCollider>();
            //     if (sc != null)
            //     {
            //         float4 sphereData = new float4();
            //         sphereData.xyz = go.transform.position;
            //         sphereData.w = sc.radius;
            //         _sphereData.Add(sc, sphereData);
            //     }
            // }

            // max count 1000
            {
                int maxTryCount = 10000;
                int maxTryIndex = 0;
                Sphere[] spheres = new Sphere[MaxCount];
                int index = 0;
                do
                {
                    maxTryIndex += 1;
                    var radius = MaxSize.x + Random.value * (MaxSize.y - MaxSize.x);
                    var c = RadianToVector2(Random.value * math.PI * 2f) * math.max(1f, MaxRange - radius) * Random.value;
                    var center = new float3(c.x, radius, c.y);

                    bool isCollision = false;
                    for (int i = 0; i < index; i++)
                    {
                        var collision = spheres[i];
                        if (math.distance(center, collision.position) < (radius + collision.radius))
                        {
                            isCollision = true;
                            break;
                        }
                    }

                    if (!isCollision)
                    {
                        spheres[index].position = center;
                        spheres[index].radius = radius;
                        var color = Random.ColorHSV();
                        var isMetal = Random.value < 0.5f;
                        spheres[index].albedo = isMetal ? float3.zero : new float3(color.r, color.g, color.b);
                        spheres[index].specular = isMetal ? new float3(color.r, color.g, color.b) : 0.04f;
                        spheres[index].smoothness = isMetal ? Random.value : 0.04f;
                        var emissionColor = Random.ColorHSV();
                        spheres[index].emission = Random.value < 0.1f ? new float3(emissionColor.r, emissionColor.g, emissionColor.b) : 0f;
                        index++;
                    }
                } while (index < MaxCount && maxTryIndex < maxTryCount);

                Array.Resize(ref spheres, index);
                _NewSphereComputeBuffer = new ComputeBuffer(index, UnsafeUtility.SizeOf<Sphere>());
                _NewSphereComputeBuffer.SetData(spheres);
            }

            if (RayTracingShader != null)
            {
                RayTracingComputeKernel = RayTracingShader.FindKernel("RayTracingCompute");
                RenderPipelineManager.endCameraRendering += Render;
            }
        }

        public static float2 RadianToVector2(float radian)
        {
            return new float2(MathF.Cos(radian), MathF.Sin(radian));
        }

        private void Update()
        {
            bool isChange = false;
            // foreach (var k in _sphereData.Keys.ToArray())
            // {
            //     var newSphereData = new float4();
            //     newSphereData.xyz = k.transform.position;
            //     newSphereData.w = k.radius;
            //
            //     if (math.any(newSphereData - _sphereData[k]))
            //     {
            //         isChange = true;
            //         _sphereData[k] = newSphereData;
            //     }
            // }

            if (transform.hasChanged || isChange)
            {
                // {
                //     Array.Resize(ref _sphereDataArray, _sphereData.Count);
                //     int i = 0;
                //     foreach (var v in _sphereData.Values)
                //     {
                //         _sphereDataArray[i] = v;
                //         i++;
                //     }
                //
                //     if (_SphereComputeBuffer == null)
                //     {
                //         _SphereComputeBuffer = new ComputeBuffer(_sphereData.Count, UnsafeUtility.SizeOf<float4>());
                //     }
                //
                //     _SphereComputeBuffer.SetData(_sphereDataArray);
                // }

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
            
            RayTracingShader.SetBuffer(RayTracingComputeKernel, _NewSphereBuffer, _NewSphereComputeBuffer);
            RayTracingShader.SetFloat(_Seed, Random.value);
        }

        private void InitRenderTexture()
        {
            if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
            {
                if (_target != null)
                {
                    _target.Release();
                }

                _target = new RenderTexture(Screen.width, Screen.height, 0,
                                            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                _target.enableRandomWrite = true;
                _target.Create();
            }

            if (_finalTarget == null || _finalTarget.width != Screen.width || _finalTarget.height != Screen.height)
            {
                if (_finalTarget != null)
                {
                    _finalTarget.Release();
                }

                _finalTarget = new RenderTexture(Screen.width, Screen.height, 0,
                                                 RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                _finalTarget.enableRandomWrite = true;
                _finalTarget.Create();
                _currentSample = 0;
            }
        }
    }
}