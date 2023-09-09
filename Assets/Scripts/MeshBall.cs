#nullable enable

using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    static int m_baseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    Mesh? m_mesh = default(Mesh);

    [SerializeField]
    Material? m_material = default(Material);

    Matrix4x4[] m_matrices   = new Matrix4x4[1023];
    Vector4[]   m_baseColors = new Vector4[1023];

    MaterialPropertyBlock? m_block;

    private void Awake()
    {
        for (int i = 0; i < m_matrices.Length; i++)
        {
            m_matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f,
                Quaternion.identity,
                Vector3.one
            );
            m_baseColors[i] = new Vector4(
                Random.value,
                Random.value,
                Random.value,
                1f
            );
        }

        StartCoroutine(ChangeBall(10));
    }

    private IEnumerator ChangeBall(int countPerFrame)
    {
        int i = 0;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            for (int j = 0; j < countPerFrame; j++)
            {
                i = (i + 1) % m_matrices.Length;
                m_matrices[i] = Matrix4x4.TRS(
                    Random.insideUnitSphere * 10f,
                    Quaternion.identity,
                    Vector3.one
                );
                m_baseColors[i] = new Vector4(
                    Random.value,
                    Random.value,
                    Random.value,
                    1f
                );
            }
        }
    }

    private void Update()
    {
        if (null == m_block)
        {
            m_block = new MaterialPropertyBlock();
            m_block.SetVectorArray(m_baseColorId, m_baseColors);
        }

        Graphics.DrawMeshInstanced(m_mesh, 0, m_material, m_matrices, 1023, m_block);
    }
}