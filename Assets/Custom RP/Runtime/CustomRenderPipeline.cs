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
            BeginFrameRendering(context, cameras);
            for (int i = 0; i < cameras.Length; i++)
            {
                RenderPipeline.BeginCameraRendering(context, cameras[i]);
                m_renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing);
                RenderPipeline.EndCameraRendering(context, cameras[i]);
            }

            EndFrameRendering(context, cameras);
        }
    }
}