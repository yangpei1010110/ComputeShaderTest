#nullable enable

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BvhBuild : MonoBehaviour
{
    private Bounds[] boundsArray = Array.Empty<Bounds>();
    private int      totalBoundsCount;
    private int      boundsIndex;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = Stopwatch.StartNew();
        totalBoundsCount = default(int);
        foreach (GameObject go in gameObject.scene.GetRootGameObjects())
        {
            ForEachGo(go, g =>
            {
                var meshFilter = g.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    return;
                }

                totalBoundsCount += meshFilter.mesh.triangles.Length / 3;
            });
        }

        boundsArray = new Bounds[totalBoundsCount];
        foreach (GameObject go in gameObject.scene.GetRootGameObjects())
        {
            ForEachGo(go, g =>
            {
                var meshFilter = g.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    return;
                }

                var mesh = meshFilter.mesh;
                boundsIndex += GetBoundsNonAlloc(g, mesh, ref boundsArray, boundsIndex);
            });
        }

        sw.Stop();
        Debug.Log($"totalBoundsCount:{totalBoundsCount}, sw:{sw.Elapsed}");
    }


    private int GetBoundsNonAlloc(in GameObject go, in Mesh mesh, ref Bounds[] bounds, in int startIndex)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        if (tris == null || verts == null)
        {
            return 0;
        }

        var boundsCount = tris.Length / 3;
        var goTransform = go.transform;
        for (int i = 0; i < boundsCount; i++)
        {
            try
            {
                var localToWorldMatrix = goTransform.localToWorldMatrix;
                var pos = goTransform.position;
                var t0 = pos + (Vector3)(localToWorldMatrix * verts[tris[i * 3 + 0]]);
                var t1 = pos + (Vector3)(localToWorldMatrix * verts[tris[i * 3 + 1]]);
                var t2 = pos + (Vector3)(localToWorldMatrix * verts[tris[i * 3 + 2]]);
                bounds[startIndex + i] = GetBounds(t0, t1, t2);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.Log($"bounds length:{bounds.Length},boundsCount {boundsCount}, startIndex:{startIndex}, i:{i}");
            }
        }

        return boundsCount;
    }

    private Bounds GetBounds(Vector3 t0, Vector3 t1, Vector3 t2)
    {
        var min = Vector3.Min(t0, Vector3.Min(t1, t2));
        var max = Vector3.Max(t0, Vector3.Max(t1, t2));
        return new Bounds((min + max) * 0.5f, max - min);
    }

    private void ForEachGo(in GameObject go, in Action<GameObject> doSome)
    {
        doSome(go);
        foreach (Transform t in go.transform)
        {
            doSome(t.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < totalBoundsCount; i++)
        {
            var bound = boundsArray[i];
            Gizmos.DrawWireCube(bound.center, bound.size);
        }
    }
}