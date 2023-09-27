#nullable enable
using UnityEngine;

namespace Custom_RP.Runtime
{
    [DisallowMultipleComponent]
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        static int m_baseColorId = Shader.PropertyToID("_BaseColor");

        private static MaterialPropertyBlock? m_block;

        [SerializeField]
        public Color baseColor = Color.white;

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (null == m_block)
            {
                m_block = new MaterialPropertyBlock();
            }

            m_block.SetColor(m_baseColorId, baseColor);
            GetComponent<Renderer>().SetPropertyBlock(m_block);
        }
    }
}