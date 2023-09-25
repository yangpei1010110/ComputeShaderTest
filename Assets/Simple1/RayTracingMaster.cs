#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Simple1
{
    public class RayTracingMaster : MonoBehaviour
    {
        public                  ComputeShader? RayTracingShader;
        private                 RenderTexture? _target;
        private                 Camera?        _camera;
        private static          int            RayTracingComputeKernel  = -1;
        private static readonly int            _CameraToWorld           = Shader.PropertyToID("_CameraToWorld");
        private static readonly int            _CameraInverseProjection = Shader.PropertyToID("_CameraInverseProjection");
        private static readonly int            Result                   = Shader.PropertyToID("Result");

        void Start()
        {
            _camera ??= GetComponent<Camera>();
            if (RayTracingShader != null)
            {
                RayTracingComputeKernel = RayTracingShader.FindKernel("RayTracingCompute");
                RenderPipelineManager.endCameraRendering += Render;
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
            Graphics.Blit(_target, c.targetTexture);
        }

        private void SetShaderParameters(Camera c)
        {
            if (RayTracingShader == null)
            {
                return;
            }

            RayTracingShader.SetTexture(RayTracingComputeKernel, Result, _target);

            RayTracingShader.SetMatrix(_CameraToWorld, c.cameraToWorldMatrix);
            RayTracingShader.SetMatrix(_CameraInverseProjection, c.projectionMatrix.inverse);
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