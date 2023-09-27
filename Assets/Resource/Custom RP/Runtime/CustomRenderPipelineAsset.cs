#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Resource.Custom_RP.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
        protected override RenderPipeline CreatePipeline()
            => new CustomRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher);
    }
}