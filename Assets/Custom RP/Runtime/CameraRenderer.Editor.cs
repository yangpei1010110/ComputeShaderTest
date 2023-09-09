#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Custom_RP.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawGizmos();
        partial void DrawUnsupportedShaders();

        #if UNITY_EDITOR
        public static ShaderTagId[] m_legacyShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        public static Material? m_errorMaterial;

        partial void DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                m_context.DrawGizmos(m_camera, GizmoSubset.PreImageEffects);
                m_context.DrawGizmos(m_camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void DrawUnsupportedShaders()
        {
            m_errorMaterial ??= new Material(Shader.Find("Hidden/InternalErrorShader"));
            var drawingSettings = new DrawingSettings(m_legacyShaderTagIds[0], new SortingSettings(m_camera))
            {
                overrideMaterial = m_errorMaterial,
            };

            for (int i = 1; i < m_legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, m_legacyShaderTagIds[i]);
            }

            var filteringSettings = FilteringSettings.defaultValue;
            m_context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        #endif
    }
}