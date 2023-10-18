#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Simple1
{
    public class RayTracingMaster : MonoBehaviour
    {
        private                 BvhBuild?      _bvhBuild;
        private                 ComputeBuffer? BvhTreeBuffer;
        private                 ComputeBuffer? BvhTreeVertices;
        private                 ComputeBuffer? BvhTreeTriangles;
        private static readonly int            BvhTreeCount   = Shader.PropertyToID("BvhTreeCount");
        private static readonly int            TrianglesCount = Shader.PropertyToID("TrianglesCount");

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
        private static readonly int            _MeshBuffer = Shader.PropertyToID("_MeshBuffer");
        private static          ComputeBuffer? _MeshComputeBuffer;
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

        void Start()
        {
            _camera ??= GetComponent<Camera>();
            _bvhBuild ??= GetComponent<BvhBuild>();
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
                        spheres[index].emission = Random.value < 0.1f
                            ? new float3(math.max(0.25f, emissionColor.r), math.max(0.25f, emissionColor.g), math.max(0.25f, emissionColor.b))
                            : 0f;
                        index++;
                    }
                } while (index < MaxCount && maxTryIndex < maxTryCount);

                Array.Resize(ref spheres, index);
                _NewSphereComputeBuffer = new ComputeBuffer(index, UnsafeUtility.SizeOf<Sphere>());
                _NewSphereComputeBuffer.SetData(spheres);
            }
            _MeshComputeBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<Vector3>());

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
            if (IsNeedRebuild && _bvhBuild != null)
            {
                isChange = true;
                _bvhBuild.Build(MeshObjects);
                if (BvhTreeBuffer == null
                 || BvhTreeBuffer.count != _bvhBuild._tree._arr.Length
                 || BvhTreeVertices == null
                 || BvhTreeVertices.count != _bvhBuild.Vertices.Length
                 || BvhTreeTriangles == null
                 || BvhTreeTriangles.count != _bvhBuild.Triangles.Length)
                {
                    BvhTreeBuffer ??= new ComputeBuffer(_bvhBuild._tree._arr.Length, UnsafeUtility.SizeOf<BvhNodeTools.BvhNode>());
                    BvhTreeVertices ??= new ComputeBuffer(_bvhBuild.Vertices.Length, UnsafeUtility.SizeOf<Vector3>());
                    BvhTreeTriangles ??= new ComputeBuffer(_bvhBuild.Triangles.Length, UnsafeUtility.SizeOf<int>());
                }

                BvhTreeBuffer.SetData(_bvhBuild._tree._arr);
                BvhTreeVertices.SetData(_bvhBuild.Vertices);
                BvhTreeTriangles.SetData(_bvhBuild.Triangles);
                // RebuildMeshObject();
                IsNeedRebuild = false;
            }

            if (transform.hasChanged || isChange)
            {
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

            RayTracingShader.SetBuffer(RayTracingComputeKernel, @"BvhTree", BvhTreeBuffer);
            RayTracingShader.SetBuffer(RayTracingComputeKernel, @"Vertices", BvhTreeVertices);
            RayTracingShader.SetBuffer(RayTracingComputeKernel, @"Triangles", BvhTreeTriangles);
            RayTracingShader.SetInt(BvhTreeCount, _bvhBuild._tree._arr.Length);
            RayTracingShader.SetInt(TrianglesCount, BvhTreeTriangles.count);

            RayTracingShader.SetTexture(RayTracingComputeKernel, _SkyboxTexture, SkyboxTexture);
            RayTracingShader.SetTexture(RayTracingComputeKernel, Result, _target);

            RayTracingShader.SetMatrix(_CameraToWorld, c.cameraToWorldMatrix);
            RayTracingShader.SetMatrix(_CameraInverseProjection, c.projectionMatrix.inverse);

            RayTracingShader.SetVector(_PixelOffset, new Vector2(Random.value, Random.value));

            RayTracingShader.SetBuffer(RayTracingComputeKernel, _NewSphereBuffer, _NewSphereComputeBuffer);
            RayTracingShader.SetBuffer(RayTracingComputeKernel, _MeshBuffer, _MeshComputeBuffer);

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

        public static  bool                IsNeedRebuild;
        private static HashSet<MeshFilter> MeshObjects  = new();
        private static Vector3[]           MeshTriangle = Array.Empty<Vector3>();

        public static void RegisterMeshObject(MeshFilter meshFilter)
        {
            if (meshFilter.sharedMesh == null)
            {
                return;
            }

            if (MeshObjects.Contains(meshFilter))
            {
                return;
            }

            MeshObjects.Add(meshFilter);
            IsNeedRebuild = true;
        }

        public static void UnregisterMeshObject(MeshFilter meshFilter)
        {
            if (meshFilter.sharedMesh == null)
            {
                return;
            }

            if (!MeshObjects.Contains(meshFilter))
            {
                return;
            }

            MeshObjects.Remove(meshFilter);
            IsNeedRebuild = true;
        }

        private void RebuildMeshObject()
        {
            var newCount = MeshObjects.Sum(m => m.sharedMesh.triangles.Length);
            Debug.Log($"newCount : {newCount}");
            if (newCount != MeshTriangle.Length)
            {
                Array.Resize(ref MeshTriangle, newCount);
            }

            int index = 0;
            foreach (MeshFilter meshObject in MeshObjects)
            {
                Mesh mesh = meshObject.mesh;
                var trans = meshObject.transform;
                var pos = trans.position;
                var localToWorldMatrix = trans.localToWorldMatrix;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    MeshTriangle[index] = pos + (Vector3)(localToWorldMatrix * vertices[triangles[i]]);
                    MeshTriangle[index + 1] = pos + (Vector3)(localToWorldMatrix * vertices[triangles[i + 1]]);
                    MeshTriangle[index + 2] = pos + (Vector3)(localToWorldMatrix * vertices[triangles[i + 2]]);
                    index += 3;
                }
            }

            if (_MeshComputeBuffer == null)
            {
                _MeshComputeBuffer = new ComputeBuffer(math.max(newCount, 1), UnsafeUtility.SizeOf<Vector3>());
            }
            else if (_MeshComputeBuffer.count != newCount)
            {
                _MeshComputeBuffer.Release();
                _MeshComputeBuffer = new ComputeBuffer(math.max(newCount, 1), UnsafeUtility.SizeOf<Vector3>());
            }

            Debug.Log($"Buffer : {_MeshComputeBuffer.count}");
            _MeshComputeBuffer.SetData(MeshTriangle);
            IsNeedRebuild = false;
        }
    }
}