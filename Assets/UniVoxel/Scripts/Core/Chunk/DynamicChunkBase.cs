using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniVoxel.Core
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public abstract class DynamicChunkBase : ChunkBase
    {
        protected MeshFilter _meshFilter;
        protected MeshRenderer _meshRenderer;
        protected MeshCollider _meshCollider;

        protected Mesh _mesh;

        protected Vector3[] _vertices;
        protected int[] _triangles;
        protected Vector2[] _uv;
        protected Vector3[] _normals;
        protected Vector4[] _tangents;

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

            _mesh.vertices = this._vertices;
            _mesh.uv = this._uv;
            _mesh.normals = this._normals;
            _mesh.tangents = this._tangents;
            _mesh.triangles = this._triangles;

            _mesh.RecalculateBounds();
        }

        protected virtual void ClearMeshProperties()
        {
            _vertices = null;
            _triangles = null;
            _uv = null;
            _normals = null;
            _tangents = null;
        }

        protected virtual void UpdateCollider()
        {
            _meshFilter.sharedMesh = null;
            _meshFilter.mesh = this._mesh;
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

        protected abstract void UpdateMeshProperties();

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
