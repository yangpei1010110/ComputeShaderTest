#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Custom_RP.Runtime
{
    public class CustomRenderPipeline : RenderPipeline
    {
        public CameraRenderer m_renderer = new();
        bool                  useDynamicBatching, useGPUInstancing;

        public CustomRenderPipeline(
            bool useDynamicBatching,
            bool useGPUInstancing,
            bool useSRPBatcher
        )
        {
            this.useDynamicBatching = useDynamicBatching;
            this.useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                m_renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing);
            }
        }
    }
}