using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace UniVoxel.Core
{

    public class JobChunkBase : ChunkBase
    {
        protected static BoxFaceSide[] FaceSides = new BoxFaceSide[] { BoxFaceSide.Front, BoxFaceSide.Back, BoxFaceSide.Top, BoxFaceSide.Bottom, BoxFaceSide.Right, BoxFaceSide.Left };
        protected MeshFilter _meshFilter;
        protected MeshRenderer _meshRenderer;
        protected MeshCollider _meshCollider;

        protected Mesh _mesh;

        protected NativeArray<Vector3> _vertices;
        protected NativeArray<int> _triangles;
        protected NativeArray<Vector2> _uv;
        protected NativeArray<Vector3> _normals;
        protected NativeArray<Vector4> _tangents;

        protected JobHandle CalculateMeshPropertiesJobHandle;

        protected override void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            InitMesh();
        }

        protected virtual void InitMesh()
        {
            _mesh = new Mesh();
            _mesh.name = $"Mesh({Name})";
            _mesh.MarkDynamic();
            _meshFilter.mesh = this._mesh;

            this._meshFilter.mesh = this._mesh;
        }

        protected virtual void UpdateRenderer()
        {
            _mesh.Clear();

            _mesh.SetVertices(_vertices);
            // _mesh.SetUVs(0, _uv);
            // _mesh.SetNormals(_normals);
            // _mesh.SetTangents(_tangents);
            // _mesh.SetTriangles(_triangles, 0);

            _mesh.RecalculateBounds();
        }

        protected virtual void ClearMeshProperties()
        {
            // _vertices.Clear();
            // _triangles.Clear();
            // _uv.Clear();
            // _normals.Clear();
            // _tangents.Clear();
        }

        protected virtual void UpdateCollider()
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = this._mesh;
        }

        protected virtual void UpdateChunk(bool updatesCollider = true)
        {
            ClearMeshProperties();

            UpdateMeshProperties();

            UpdateRenderer();

            if (updatesCollider)
            {
                UpdateCollider();
            }
        }

        protected void UpdateMeshProperties()
        {
            
        }

        protected virtual void Update()
        {
            if (NeedsUpdate)
            {
                this.NeedsUpdate = false;
                UpdateChunk(true);
            }
        }
    }
}