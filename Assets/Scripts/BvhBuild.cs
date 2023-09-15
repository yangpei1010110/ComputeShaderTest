#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BvhBuild : MonoBehaviour
{
    [Range(0, 20)] public int                             showDepth;
    public                ArrayTree<BvhNodeTools.BvhNode> _tree     = new();
    public                Vector3[]                       Vertices  = Array.Empty<Vector3>();
    public                int[]                       Triangles = Array.Empty<int>();

    void Start()
    {
        Stopwatch sw = Stopwatch.StartNew();
        List<BvhNodeTools.BvhNode> bvhNodeList = new();
        List<Vector3> verticesList = new();
        List<int> trianglesList = new();
        foreach (GameObject go in gameObject.scene.GetRootGameObjects())
        {
            BvhNodeTools.SubCollectAllBvhNodes(go, ref bvhNodeList, ref verticesList,ref trianglesList);
        }

        var _bvhNodes = bvhNodeList.ToArray();
        Vertices = verticesList.ToArray();
        Triangles = trianglesList.ToArray();
        BvhNodeTools.Build(_bvhNodes, 0, _bvhNodes.Length, _tree, 0);

        sw.Stop();
        Debug.Log($"三角形数量:{_bvhNodes.Length}, 构建时间:{sw.ElapsedMilliseconds} ms");
        if (_tree._arr.Length != 0)
        {
            var maxDept = _tree.depth;
            Debug.Log($"maxDept:{maxDept}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_tree._arr.Length != 0)
        {
            DrawWireCube(_tree, 0, 0);
        }
    }

    private void DrawWireCube(in ArrayTree<BvhNodeTools.BvhNode> tree, in int index, in int depth)
    {
        if (depth == showDepth && !tree.IsNull(index))
        {
            Gizmos.DrawWireCube(tree[index].value.center, tree[index].value.size);
        }
        else if (tree.IsNull(tree.Left(index)) && tree.IsNull(tree.Right(index)) && depth < showDepth && !tree.IsNull(index))
        {
            Gizmos.DrawWireCube(tree[index].value.center, tree[index].value.size);
        }
        else
        {
            if (!tree.IsNull(tree.Left(index)))
            {
                DrawWireCube(tree, tree.Left(index), depth + 1);
            }

            if (!tree.IsNull(tree.Right(index)))
            {
                DrawWireCube(tree, tree.Right(index), depth + 1);
            }
        }
    }
}