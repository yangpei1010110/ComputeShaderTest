#nullable enable
using Resource.Scripts;
using Resource.Scripts.Tools;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resource.Custom_RP.Runtime.Shaders
{
    public class RayTracingCompute : MonoBehaviour
    {
        public                  ComputeShader? RayTracingShader;
        private                 RenderTexture? _target;
        private                 RenderTexture? _realTarget;
        private                 Camera?        _camera;
        private static readonly int            CameraToWorld           = Shader.PropertyToID("_CameraToWorld");
        private static readonly int            CameraInverseProjection = Shader.PropertyToID("_CameraInverseProjection");
        private static readonly int            Result                  = Shader.PropertyToID("Result");
        private static readonly int            RealResult              = Shader.PropertyToID("RealResult");
        private static readonly int            BvhTree                 = Shader.PropertyToID("BvhTree");
        private static readonly int            Vertices                = Shader.PropertyToID("Vertices");
        private static readonly int            Triangles               = Shader.PropertyToID("Triangles");
        private static readonly int            BvhTreeCount            = Shader.PropertyToID("BvhTreeCount");
        private static readonly int            TrianglesCount          = Shader.PropertyToID("TrianglesCount");
        private static readonly int            _SkyboxTexture          = Shader.PropertyToID("_SkyboxTexture");
        private static readonly int            _debugDiffuseAlbedo     = Shader.PropertyToID("_debugDiffuseAlbedo");
        private static readonly int            _time                   = Shader.PropertyToID("_time");
        private static readonly int            _seed                   = Shader.PropertyToID("_seed");
        private static readonly int _frameCount = Shader.PropertyToID("_frameCount");
        private                 Vector3        _debugDiffuseAlbedoColor;
        public                  Texture2D      _skyboxTexture;
        private                 ComputeBuffer? BvhTreeBuffer;
        private                 ComputeBuffer? BvhTreeVertices;
        private                 ComputeBuffer? BvhTreeTriangles;
        private static          int            RayTracingComputeKernel = -1;

        private BvhBuild? _bvhBuild;
        private bool      _bvhIsChanged = true;

        private void OnEnable()
        {
            _camera ??= GetComponent<Camera>();
            _bvhBuild ??= GetComponent<BvhBuild>();
            _debugDiffuseAlbedoColor = new Vector3(Random.value * Random.value,
                                                   Random.value * Random.value,
                                                   Random.value * Random.value);
            _debugDiffuseAlbedoColor = Vector3.one * 0.5f;
            if (_realTarget != null)
            {
                _realTarget.Release();
            }
            else
            {
                _realTarget = new RenderTexture(Screen.width, Screen.height, 0,
                                                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                _realTarget.enableRandomWrite = true;
                _realTarget.Create();
            }

            RenderPipelineManager.endCameraRendering += Render;
        }

        private void SetShaderParameters(Camera c)
        {
            if (RayTracingShader == null)
            {
                return;
            }

            if (_bvhBuild == null)
            {
                return;
            }

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

            if (_bvhIsChanged)
            {
                BvhTreeBuffer.SetData(_bvhBuild._tree._arr);
                BvhTreeVertices.SetData(_bvhBuild.Vertices);
                BvhTreeTriangles.SetData(_bvhBuild.Triangles);
                _bvhIsChanged = false;

                RayTracingShader.SetTexture(RayTracingComputeKernel, _SkyboxTexture, _skyboxTexture);
                RayTracingShader.SetBuffer(RayTracingComputeKernel, Vertices, BvhTreeVertices);
                RayTracingShader.SetBuffer(RayTracingComputeKernel, Triangles, BvhTreeTriangles);
                RayTracingShader.SetBuffer(RayTracingComputeKernel, BvhTree, BvhTreeBuffer);
                RayTracingShader.SetInt(BvhTreeCount, _bvhBuild._tree._arr.Length);
                RayTracingShader.SetInt(TrianglesCount, BvhTreeTriangles.count);
                RayTracingShader.SetMatrix(CameraToWorld, c.cameraToWorldMatrix);
                RayTracingShader.SetMatrix(CameraInverseProjection, c.projectionMatrix.inverse);
                RayTracingShader.SetVector(_debugDiffuseAlbedo, _debugDiffuseAlbedoColor);
                RayTracingShader.SetVector(_time, new Vector4(Time.time, Time.time * 0.25f, Time.time * 0.125f, 0));
                RayTracingShader.SetFloat(_seed, Random.value);
                RayTracingShader.SetInt(_frameCount, Time.frameCount);
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

            if (RayTracingComputeKernel <= -1)
            {
                RayTracingComputeKernel = RayTracingShader.FindKernel("RayTracingCompute");
            }

            SetShaderParameters(c);
            InitRenderTexture();
            RayTracingShader.SetTexture(RayTracingComputeKernel, Result, _target);
            RayTracingShader.SetTexture(RayTracingComputeKernel, RealResult, _realTarget);
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            RayTracingShader.Dispatch(RayTracingComputeKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(_target, c.targetTexture);
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

            if (_realTarget == null)
            {
                _realTarget = new RenderTexture(Screen.width, Screen.height, 0,
                                                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                _realTarget.enableRandomWrite = true;
                _realTarget.Create();
            }
        }
    }
}