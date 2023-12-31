#nullable enable
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Tools
{
    public static class BvhNodeTools
    {
        public struct BvhNode
        {
            public Bounds value;
            public int    gameObjectId;
            public int    materialId;
            public int    triangleIndex;
        }

        public enum XYZ
        {
            Empty = 0,
            X     = 1 << 0,
            Y     = 1 << 1,
            Z     = 1 << 2,
        }

        private static Comparer<BvhNode> xComparer = Comparer<BvhNode>.Create((left, right) => { return left.value.center.x.CompareTo(right.value.center.x); });
        private static Comparer<BvhNode> yComparer = Comparer<BvhNode>.Create((left, right) => { return left.value.center.y.CompareTo(right.value.center.y); });
        private static Comparer<BvhNode> zComparer = Comparer<BvhNode>.Create((left, right) => { return left.value.center.z.CompareTo(right.value.center.z); });

        public static void Build(in BvhNode[] arr, in int start, in int length, in ArrayTree<BvhNode> tree, in int treeIndex)
        {
            tree[treeIndex] = new BvhNode();

            if (length <= 0)
            {
                throw new Exception("size <= 0");
            }
            else if (length == 1)
            {
                tree[treeIndex] = arr[start];
                tree[treeIndex].value = arr[start].value;
                tree[treeIndex].gameObjectId = arr[start].gameObjectId;
                tree[treeIndex].triangleIndex = arr[start].triangleIndex;
            }
            else if (length == 2)
            {
                tree[treeIndex].value = Union(arr[start].value, arr[start + 1].value);
                tree[tree.LeftAndResize(treeIndex)] = arr[start];
                tree[tree.RightAndResize(treeIndex)] = arr[start + 1];
            }
            else
            {
                Bounds centroidBounds = default(Bounds);
                centroidBounds.center = arr[start].value.center;
                for (int i = 1; i < length; i++)
                {
                    centroidBounds = Union(centroidBounds, arr[start + i].value.center);
                }

                XYZ dim = MaxExtent(centroidBounds);
                switch (dim)
                {
                    case XYZ.X:
                        Array.Sort(arr, start, length, xComparer);
                        break;
                    case XYZ.Y:
                        Array.Sort(arr, start, length, yComparer);
                        break;
                    case XYZ.Z:
                        Array.Sort(arr, start, length, zComparer);
                        break;
                    default:
                        throw new Exception("dim is empty");
                }

                int begin = start;
                int mid = begin + length / 2;
                int end = start + length;

                Build(arr, begin, mid - begin, tree, tree.LeftAndResize(treeIndex));
                Build(arr, mid, end - mid, tree, tree.RightAndResize(treeIndex));
                tree[treeIndex].value = Union(tree[tree.Left(treeIndex)].value, tree[tree.Right(treeIndex)].value);
            }
        }

        public static void SubCollectAllBvhNodes(in GameObject go, ref List<BvhNode> result, ref List<Vector3> verticesList, ref List<int> trianglesList)
        {
            MeshFilter? meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }
            SubCollectAllBvhNodes(meshFilter, ref result, ref verticesList, ref trianglesList);
        }
        public static void SubCollectAllBvhNodes(in MeshFilter meshFilter, ref List<BvhNode> result, ref List<Vector3> verticesList, ref List<int> trianglesList)
        {
            Mesh? mesh = meshFilter.mesh;
            Vector3[]? vertices = mesh.vertices;
            Vector3[]? normals = mesh.normals;
            int[]? triangles = mesh.triangles;
            Vector2[]? uvs = mesh.uv;
            if (triangles == null || normals == null || vertices == null || uvs == null)
            {
                return;
            }

            Transform goTransform = meshFilter.transform;
            Matrix4x4 goLocalToWorldMatrix = goTransform.localToWorldMatrix;
            Vector3 goPosition = goTransform.position;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = goPosition + (Vector3)(goLocalToWorldMatrix * vertices[i]);
            }

            for (int i = 0; i < triangles.Length / 3; i++)
            {
                Vector3 t0 = vertices[triangles[i * 3 + 0]];
                Vector3 t1 = vertices[triangles[i * 3 + 1]];
                Vector3 t2 = vertices[triangles[i * 3 + 2]];
                result.Add(new BvhNode()
                {
                    value = GetBounds(t0, t1, t2),
                    gameObjectId = meshFilter.GetInstanceID(),
                    triangleIndex = trianglesList.Count / 3 + i,
                });
            }


            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] += verticesList.Count;
            }

            verticesList.AddRange(vertices);
            trianglesList.AddRange(triangles);
            foreach (GameObject subGo in meshFilter.transform)
            {
                SubCollectAllBvhNodes(subGo, ref result, ref verticesList, ref trianglesList);
            }
        }

        public static Bounds Union(in BvhNode[] arr, in int start, in int size)
        {
            Bounds result = default(Bounds);
            for (int i = 0; i < size; i++)
            {
                result = Union(result, arr[start + i].value);
            }

            return result;
        }

        public static float SurfaceArea(in Bounds b0)
        {
            Vector3 size = b0.extents * 2f;
            return 2f * (size.x * size.y + size.x * size.z + size.y * size.z);
        }

        public static XYZ MaxExtent(in Bounds b0)
        {
            Vector3 diagonal = math.abs(b0.size);
            if (diagonal.x > diagonal.y && diagonal.x > diagonal.z)
            {
                return XYZ.X;
            }
            else if (diagonal.y > diagonal.z)
            {
                return XYZ.Y;
            }
            else
            {
                return XYZ.Z;
            }
        }

        public static Bounds Union(in Bounds b0, in Vector3 p0)
        {
            Vector3 min = Vector3.Min(b0.center - b0.extents, p0);
            Vector3 max = Vector3.Max(b0.center + b0.extents, p0);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static Bounds Union(in Bounds b0, in Bounds b1)
        {
            Vector3 min = Vector3.Min(b0.center - b0.extents, b1.center - b1.extents);
            Vector3 max = Vector3.Max(b0.center + b0.extents, b1.center + b1.extents);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static Bounds GetBounds(in Vector3 t0, in Vector3 t1, in Vector3 t2)
        {
            Vector3 min = Vector3.Min(t0, Vector3.Min(t1, t2));
            Vector3 max = Vector3.Max(t0, Vector3.Max(t1, t2));
            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}