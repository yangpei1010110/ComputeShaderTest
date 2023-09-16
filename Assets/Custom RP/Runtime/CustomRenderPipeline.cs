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
                BeginCameraRendering(context, cameras[i]);
                // TODO 这里是原始的渲染代码，我们需要将其替换为我们自己的渲染代码
                // m_renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing);
                EndCameraRendering(context, cameras[i]);
            }

            EndFrameRendering(context, cameras);
        }
    }
}