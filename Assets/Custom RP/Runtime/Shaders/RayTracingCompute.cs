#nullable enable
using Tools;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Custom_RP.Runtime.Shaders
{
    public class RayTracingCompute : MonoBehaviour
    {
        public                  ComputeShader? RayTracingShader;
        private                 RenderTexture? _target;
        private                 Camera?        _camera;
        private static readonly int            CameraToWorld           = Shader.PropertyToID("_CameraToWorld");
        private static readonly int            CameraInverseProjection = Shader.PropertyToID("_CameraInverseProjection");
        private static readonly int            Result                  = Shader.PropertyToID("Result");
        private static readonly int            BvhTree                 = Shader.PropertyToID("BvhTree");
        private static readonly int            Vertices                = Shader.PropertyToID("Vertices");
        private static readonly int            BvhTreeCount            = Shader.PropertyToID("BvhTreeCount");
        private                 ComputeBuffer? BvhTreeBuffer;
        private                 ComputeBuffer? BvhTreeVertices;
        private static          int            CSMain = -1;

        private BvhBuild? _bvhBuild;
        private bool      _bvhIsChanged = true;

        private void OnEnable()
        {
            Debug.Log($"Size of BvhNode: {UnsafeUtility.SizeOf<BvhNodeTools.BvhNode>()}");
            _camera ??= GetComponent<Camera>();
            _bvhBuild ??= GetComponent<BvhBuild>();
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
             || BvhTreeVertices.count != _bvhBuild.Vertices.Length)
            {
                BvhTreeBuffer ??= new ComputeBuffer(_bvhBuild._tree._arr.Length, UnsafeUtility.SizeOf<BvhNodeTools.BvhNode>());
                BvhTreeVertices ??= new ComputeBuffer(_bvhBuild.Vertices.Length, UnsafeUtility.SizeOf<Vector3>());
            }

            if (_bvhIsChanged)
            {
                BvhTreeBuffer.SetData(_bvhBuild._tree._arr);
                BvhTreeVertices.SetData(_bvhBuild.Vertices);
                _bvhIsChanged = false;
            }

            RayTracingShader.SetBuffer(CSMain, Vertices, BvhTreeVertices);
            RayTracingShader.SetBuffer(CSMain, BvhTree, BvhTreeBuffer);
            RayTracingShader.SetInt(BvhTreeCount, _bvhBuild._tree._arr.Length);
            RayTracingShader.SetMatrix(CameraToWorld, c.cameraToWorldMatrix);
            RayTracingShader.SetMatrix(CameraInverseProjection, c.projectionMatrix.inverse);
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

            if (CSMain <= -1)
            {
                CSMain = RayTracingShader.FindKernel("CSMain");
            }

            SetShaderParameters(c);
            InitRenderTexture();
            RayTracingShader.SetTexture(0, Result, _target);
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            RayTracingShader.Dispatch(CSMain, threadGroupsX, threadGroupsY, 1);
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
        }
    }
}