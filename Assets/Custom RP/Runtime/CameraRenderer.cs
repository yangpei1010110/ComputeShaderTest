#nullable enable
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Custom_RP.Runtime
{
    partial class CameraRenderer
    {
        public ScriptableRenderContext m_context;
        public Camera                  m_camera;

        public        CullingResults m_cullingResults;
        public static ShaderTagId    m_unlitShaderTagId = new("SRPDefaultUnlit");

        public const string        m_bufferName = "Render Camera";
        public       CommandBuffer m_buffer     = new() { name = m_bufferName, };

        public void Render(ScriptableRenderContext context,
                           Camera                  camera,
                           bool                    useDynamicBatching,
                           bool                    useGPUInstancing
        )
        {
            m_context = context;
            m_camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull())
            {
                return;
            }

            Setup();
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmos();
            Submit();
        }

        private bool Cull()
        {
            if (m_camera.TryGetCullingParameters(out var p))
            {
                m_cullingResults = m_context.Cull(ref p);
                return true;
            }

            return false;
        }

        private void Setup()
        {
            m_context.SetupCameraProperties(m_camera);
            CameraClearFlags flags = m_camera.clearFlags;
            m_buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? m_camera.backgroundColor.linear : Color.clear
            );
            m_buffer.BeginSample(m_sampleName);
            ExecuteBuffer();
        }

        private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            var sortingSettings = new SortingSettings(m_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };

            var drawingSettings = new DrawingSettings(m_unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            m_context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings);

            m_context.DrawSkybox(m_camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            m_context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void ExecuteBuffer()
        {
            m_context.ExecuteCommandBuffer(m_buffer);
            m_buffer.Clear();
        }

        private void Submit()
        {
            m_buffer.EndSample(m_sampleName);
            ExecuteBuffer();
            m_context.Submit();
        }

        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();
        #if UNITY_EDITOR
        string m_sampleName { get; set; }

        partial void PrepareForSceneWindow()
        {
            if (m_camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(m_camera);
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            m_buffer.name = m_sampleName = m_camera.name;
            Profiler.EndSample();
        }
        #else
        string m_sampleName => m_bufferName;
        #endif
    }
}