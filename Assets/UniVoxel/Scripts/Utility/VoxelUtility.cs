using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    public static class VoxelUtility
    {
        static readonly int FaceVertexLength = 4;
        static readonly int FaceTriangleLength = 6;

        public static int GetFaceVertexLength()
        {
            return FaceVertexLength;
        }

        public static int GetFaceTriangleLength()
        {
            return FaceTriangleLength;
        }

        static void ReserveVerticesForFaces(int NumFace, ref Vector3[] vertices)
        {
            if (NumFace <= 0)
            {
                return;
            }

            if (vertices == null)
            {
                vertices = new Vector3[FaceVertexLength * NumFace];
                return;
            }

            Array.Resize(ref vertices, vertices.Length + FaceVertexLength * NumFace);
        }

        static void ReserveTrianglesForFaces(int NumFace, ref int[] triangles)
        {
            if (NumFace <= 0)
            {
                return;
            }

            if (triangles == null)
            {
                triangles = new int[FaceTriangleLength * NumFace];
                return;
            }

            Array.Resize(ref triangles, triangles.Length + FaceTriangleLength * NumFace);
        }

        static void ReserveUVForFaces(int NumFace, ref Vector2[] uv0)
        {
            if (NumFace <= 0)
            {
                return;
            }

            if (uv0 == null)
            {
                uv0 = new Vector2[FaceVertexLength * NumFace];
                return;
            }

            Array.Resize(ref uv0, uv0.Length + FaceVertexLength * NumFace);
        }

        static void ReserveNormalsForFaces(int NumFace, ref Vector3[] normals)
        {
            if (NumFace <= 0)
            {
                return;
            }

            if (normals == null)
            {
                normals = new Vector3[FaceVertexLength * NumFace];
                return;
            }

            Array.Resize(ref normals, normals.Length + FaceVertexLength * NumFace);
        }

        static void ReserveTangentsForFaces(int NumFace, ref Vector4[] tangents)
        {
            if (NumFace <= 0)
            {
                return;
            }

            if (tangents == null)
            {
                tangents = new Vector4[FaceVertexLength * NumFace];
                return;
            }

            Array.Resize(ref tangents, tangents.Length + FaceVertexLength * NumFace);
        }

        public static void ReserveMeshForFaces(int NumFace, ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uv0, ref Vector3[] normals, ref Vector4[] tangents)
        {
            ReserveVerticesForFaces(NumFace, ref vertices);
            ReserveTrianglesForFaces(NumFace, ref triangles);
            ReserveUVForFaces(NumFace, ref uv0);
            ReserveNormalsForFaces(NumFace, ref normals);
            ReserveTangentsForFaces(NumFace, ref tangents);
        }


        static void AddVerticesForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, Vector3[] vertices, int startIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(vertices != null);
            UnityEngine.Assertions.Assert.IsTrue(vertices.Length >= startIndex + FaceVertexLength);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    vertices[startIndex] = Center + new Vector3(Extent, -Extent, Extent);
                    vertices[startIndex + 1] = Center + new Vector3(-Extent, -Extent, Extent);
                    vertices[startIndex + 2] = Center + new Vector3(Extent, Extent, Extent);
                    vertices[startIndex + 3] = Center + new Vector3(-Extent, Extent, Extent);
                    break;
                case BoxFaceSide.Back:
                    vertices[startIndex] = Center + new Vector3(-Extent, -Extent, -Extent);
                    vertices[startIndex + 1] = Center + new Vector3(Extent, -Extent, -Extent);
                    vertices[startIndex + 2] = Center + new Vector3(-Extent, Extent, -Extent);
                    vertices[startIndex + 3] = Center + new Vector3(Extent, Extent, -Extent);
                    break;
                case BoxFaceSide.Top:
                    vertices[startIndex] = Center + new Vector3(-Extent, Extent, -Extent);
                    vertices[startIndex + 1] = Center + new Vector3(Extent, Extent, -Extent);
                    vertices[startIndex + 2] = Center + new Vector3(-Extent, Extent, Extent);
                    vertices[startIndex + 3] = Center + new Vector3(Extent, Extent, Extent);
                    break;
                case BoxFaceSide.Bottom:
                    vertices[startIndex] = Center + new Vector3(Extent, -Extent, -Extent);
                    vertices[startIndex + 1] = Center + new Vector3(-Extent, -Extent, -Extent);
                    vertices[startIndex + 2] = Center + new Vector3(Extent, -Extent, Extent);
                    vertices[startIndex + 3] = Center + new Vector3(-Extent, -Extent, Extent);
                    break;
                case BoxFaceSide.Right:
                    vertices[startIndex] = Center + new Vector3(Extent, -Extent, -Extent);
                    vertices[startIndex + 1] = Center + new Vector3(Extent, -Extent, Extent);
                    vertices[startIndex + 2] = Center + new Vector3(Extent, Extent, -Extent);
                    vertices[startIndex + 3] = Center + new Vector3(Extent, Extent, Extent);
                    break;
                case BoxFaceSide.Left:
                    vertices[startIndex] = Center + new Vector3(-Extent, -Extent, Extent);
                    vertices[startIndex + 1] = Center + new Vector3(-Extent, -Extent, -Extent);
                    vertices[startIndex + 2] = Center + new Vector3(-Extent, Extent, Extent);
                    vertices[startIndex + 3] = Center + new Vector3(-Extent, Extent, -Extent);
                    break;
            }
        }


        static void AddVerticesForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, List<Vector3> vertices)
        {
            UnityEngine.Assertions.Assert.IsTrue(vertices != null);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    vertices.Add(Center + new Vector3(Extent, -Extent, Extent));
                    vertices.Add(Center + new Vector3(-Extent, -Extent, Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, Extent));
                    vertices.Add(Center + new Vector3(-Extent, Extent, Extent));
                    break;
                case BoxFaceSide.Back:
                    vertices.Add(Center + new Vector3(-Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(-Extent, Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, -Extent));
                    break;
                case BoxFaceSide.Top:
                    vertices.Add(Center + new Vector3(-Extent, Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, -Extent));
                    vertices.Add(Center + new Vector3(-Extent, Extent, Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, Extent));
                    break;
                case BoxFaceSide.Bottom:
                    vertices.Add(Center + new Vector3(Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(-Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, -Extent, Extent));
                    vertices.Add(Center + new Vector3(-Extent, -Extent, Extent));
                    break;
                case BoxFaceSide.Right:
                    vertices.Add(Center + new Vector3(Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, -Extent, Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, -Extent));
                    vertices.Add(Center + new Vector3(Extent, Extent, Extent));
                    break;
                case BoxFaceSide.Left:
                    vertices.Add(Center + new Vector3(-Extent, -Extent, Extent));
                    vertices.Add(Center + new Vector3(-Extent, -Extent, -Extent));
                    vertices.Add(Center + new Vector3(-Extent, Extent, Extent));
                    vertices.Add(Center + new Vector3(-Extent, Extent, -Extent));
                    break;
            }
        }


        static void AddTrianglesForBoxFace(BoxFaceSide FaceSide, int[] triangles, int vertexStartIndex, int triangleStartIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(triangles != null);
            UnityEngine.Assertions.Assert.IsTrue(triangles.Length >= triangleStartIndex + FaceTriangleLength);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    // up side triangle
                    triangles[triangleStartIndex] = vertexStartIndex;
                    triangles[triangleStartIndex + 1] = vertexStartIndex + 2;
                    triangles[triangleStartIndex + 2] = vertexStartIndex + 1;

                    // down side triangle
                    triangles[triangleStartIndex + 3] = vertexStartIndex + 2;
                    triangles[triangleStartIndex + 4] = vertexStartIndex + 3;
                    triangles[triangleStartIndex + 5] = vertexStartIndex + 1;
                    break;
            }
        }

        static void AddTrianglesForBoxFace(BoxFaceSide FaceSide, List<int>triangles, int vertexStartIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(triangles != null);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    // up side triangle
                    triangles.Add( vertexStartIndex);
                    triangles.Add( vertexStartIndex + 2);
                    triangles.Add( vertexStartIndex + 1);

                    // down side triangle
                    triangles.Add( vertexStartIndex + 2);
                    triangles.Add( vertexStartIndex + 3);
                    triangles.Add( vertexStartIndex + 1);
                    break;
            }
        }

        static void AddUVForBoxFace(BoxFaceSide FaceSide, Vector2[] uv0, Vector2 UVCoord00, Vector2 UVCoord11, int startIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(uv0 != null);
            UnityEngine.Assertions.Assert.IsTrue(uv0.Length >= startIndex + FaceVertexLength);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    uv0[startIndex] = new Vector2(UVCoord00.x, UVCoord00.y);
                    uv0[startIndex + 1] = new Vector2(UVCoord11.x, UVCoord00.y);
                    uv0[startIndex + 2] = new Vector2(UVCoord00.x, UVCoord11.y);
                    uv0[startIndex + 3] = new Vector2(UVCoord11.x, UVCoord11.y);
                    break;
            }
        }

        static void AddUVForBoxFace(BoxFaceSide FaceSide, List<Vector2> uv0, Vector2 UVCoord00, Vector2 UVCoord11)
        {
            UnityEngine.Assertions.Assert.IsTrue(uv0 != null);

            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    uv0.Add( new Vector2(UVCoord00.x, UVCoord00.y));
                    uv0.Add( new Vector2(UVCoord11.x, UVCoord00.y));
                    uv0.Add( new Vector2(UVCoord00.x, UVCoord11.y));
                    uv0.Add( new Vector2(UVCoord11.x, UVCoord11.y));
                    break;
            }
        }

        static void AddNormalsForBoxFace(BoxFaceSide FaceSide, Vector3[] normals, int startIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(normals != null);
            UnityEngine.Assertions.Assert.IsTrue(normals.Length >= startIndex + FaceVertexLength);

            Vector3 n;
            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    n = Vector3.forward;
                    break;
                case BoxFaceSide.Back:
                    n = Vector3.back;
                    break;
                case BoxFaceSide.Top:
                    n = Vector3.up;
                    break;
                case BoxFaceSide.Bottom:
                    n = Vector3.down;
                    break;
                case BoxFaceSide.Right:
                    n = Vector3.right;
                    break;
                case BoxFaceSide.Left:
                    n = Vector3.left;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            for (int i = 0; i < FaceVertexLength; i++)
            {
                normals[startIndex + i] = n;
            }
        }

        static void AddNormalsForBoxFace(BoxFaceSide FaceSide, List<Vector3> normals)
        {
            UnityEngine.Assertions.Assert.IsTrue(normals != null);

            Vector3 n;
            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    n = Vector3.forward;
                    break;
                case BoxFaceSide.Back:
                    n = Vector3.back;
                    break;
                case BoxFaceSide.Top:
                    n = Vector3.up;
                    break;
                case BoxFaceSide.Bottom:
                    n = Vector3.down;
                    break;
                case BoxFaceSide.Right:
                    n = Vector3.right;
                    break;
                case BoxFaceSide.Left:
                    n = Vector3.left;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            for (int i = 0; i < FaceVertexLength; i++)
            {
                normals.Add(n);
            }
        }

        static void AddTangentsForBoxFace(BoxFaceSide FaceSide, Vector4[] tangents, int startIndex)
        {
            UnityEngine.Assertions.Assert.IsTrue(tangents != null);
            UnityEngine.Assertions.Assert.IsTrue(tangents.Length >= startIndex + FaceVertexLength);

            Vector4 t;
            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    t = new Vector4(1, 0, 0, 1);
                    break;
                case BoxFaceSide.Back:
                    t = new Vector4(-1, 0, 0, 1);
                    break;
                case BoxFaceSide.Top:
                    t = new Vector4(-1, 0, 0, 1);
                    break;
                case BoxFaceSide.Bottom:
                    t = new Vector4(1, 0, 0, 1);
                    break;
                case BoxFaceSide.Right:
                    t = new Vector4(0, 0, -1, 1);
                    break;
                case BoxFaceSide.Left:
                    t = new Vector4(0, 0, 1, 1);
                    break;
                default:
                    throw new System.ArgumentException();
            }

            for (int i = 0; i < FaceVertexLength; i++)
            {
                tangents[startIndex + i] = t;
            }
        }

        static void AddTangentsForBoxFace(BoxFaceSide FaceSide, List<Vector4> tangents)
        {
            UnityEngine.Assertions.Assert.IsTrue(tangents != null);

            Vector4 t;
            switch (FaceSide)
            {
                case BoxFaceSide.Front:
                    t = new Vector4(1, 0, 0, 1);
                    break;
                case BoxFaceSide.Back:
                    t = new Vector4(-1, 0, 0, 1);
                    break;
                case BoxFaceSide.Top:
                    t = new Vector4(-1, 0, 0, 1);
                    break;
                case BoxFaceSide.Bottom:
                    t = new Vector4(1, 0, 0, 1);
                    break;
                case BoxFaceSide.Right:
                    t = new Vector4(0, 0, -1, 1);
                    break;
                case BoxFaceSide.Left:
                    t = new Vector4(0, 0, 1, 1);
                    break;
                default:
                    throw new System.ArgumentException();
            }

            for (int i = 0; i < FaceVertexLength; i++)
            {
                tangents.Add(t);
            }
        }

        public static void AddMeshForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, Vector3[] vertices, int[] triangles, Vector2[] uv0, Vector2 UVCoord00, Vector2 UVCoord11, Vector3[] normals, Vector4[] tangents, int vertexStartIndex, int triangleStartIndex)
        {
            AddVerticesForBoxFace(FaceSide, Center, Extent, vertices, vertexStartIndex);
            AddTrianglesForBoxFace(FaceSide, triangles, vertexStartIndex, triangleStartIndex);
            AddUVForBoxFace(FaceSide, uv0, UVCoord00, UVCoord11, vertexStartIndex);
            AddNormalsForBoxFace(FaceSide, normals, vertexStartIndex);
            AddTangentsForBoxFace(FaceSide, tangents, vertexStartIndex);
        }

        public static void AddMeshForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, List<Vector3> vertices, List<int> triangles, List<Vector2> uv0, Vector2 UVCoord00, Vector2 UVCoord11, List<Vector3> normals, List<Vector4> tangents, int vertexStartIndex)
        {
            AddVerticesForBoxFace(FaceSide, Center, Extent, vertices);
            AddTrianglesForBoxFace(FaceSide, triangles, vertexStartIndex);
            AddUVForBoxFace(FaceSide, uv0, UVCoord00, UVCoord11);
            AddNormalsForBoxFace(FaceSide, normals);
            AddTangentsForBoxFace(FaceSide, tangents);
        }

        public static void AddMeshForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, List<Vector3> vertices, List<int> triangles, List<Vector2> uv0, Vector2 UVCoord00, Vector2 UVCoord11, List<Vector3> normals, List<Vector4> tangents)
        {
            var numVertex = vertices.Count;
            AddMeshForBoxFace(FaceSide, Center, Extent, vertices, triangles, uv0, UVCoord00, UVCoord11, normals, tangents, numVertex);
        }

        public static void ReserveAndAddMeshForBoxFace(BoxFaceSide FaceSide, Vector3 Center, float Extent, ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uv0, Vector2 UVCoord00, Vector2 UVCoord11, ref Vector3[] normals, ref Vector4[] tangents)
        {
            var numVertices = vertices.Length;
            var numTriangles = triangles.Length;
            ReserveMeshForFaces(1, ref vertices, ref triangles, ref uv0, ref normals, ref tangents);
            AddMeshForBoxFace(FaceSide, Center, Extent, vertices, triangles, uv0, UVCoord00, UVCoord11, normals, tangents, numVertices, numTriangles);
        }

        public static void AddBox(Vector3 Center, float Extent, Vector3[] vertices, int[] triangles, Vector2[] uv0, Vector2 UVCoord00, Vector2 UVCoord11, Vector3[] normals, Vector4[] tangents, int vertexStartIndex, int triangleStartIndex)
        {
            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                AddMeshForBoxFace(side, Center, Extent, vertices, triangles, uv0, UVCoord00, UVCoord11, normals, tangents, vertexStartIndex, triangleStartIndex);
                vertexStartIndex += FaceVertexLength;
                triangleStartIndex += FaceTriangleLength;
            }
        }

        public static void AddBox(Vector3 Center, float Extent, List<Vector3> vertices, List<int> triangles, List<Vector2> uv0, Vector2 UVCoord00, Vector2 UVCoord11, List<Vector3> normals, List<Vector4> tangents)
        {
            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                AddMeshForBoxFace(side, Center, Extent, vertices, triangles, uv0, UVCoord00, UVCoord11, normals, tangents);
            }
        }

        public static void ReserveAndAddBox(Vector3 Center, float Extent, ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uv0, Vector2 UVCoord00, Vector2 UVCoord11, ref Vector3[] normals, ref Vector4[] tangents)
        {
            var numVertices = vertices.Length;
            var numTriangles = triangles.Length;
            ReserveMeshForFaces(6, ref vertices, ref triangles, ref uv0, ref normals, ref tangents);
            AddBox(Center, Extent, vertices, triangles, uv0, UVCoord00, UVCoord11, normals, tangents, numTriangles, numTriangles);
        }
    }
}