﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

namespace UniVoxel.Core
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public abstract class DynamicChunkBase : ChunkBase
    {
        protected static BoxFaceSide[] FaceSides = new BoxFaceSide[] { BoxFaceSide.Front, BoxFaceSide.Back, BoxFaceSide.Top, BoxFaceSide.Bottom, BoxFaceSide.Right, BoxFaceSide.Left };
        protected MeshFilter _meshFilter;
        protected MeshRenderer _meshRenderer;
        protected MeshCollider _meshCollider;

        protected Mesh _mesh;

        protected List<Vector3> _vertices = new List<Vector3>();
        protected List<int> _triangles = new List<int>();
        protected List<Vector2> _uv = new List<Vector2>();
        protected List<Vector3> _normals = new List<Vector3>();
        protected List<Vector4> _tangents = new List<Vector4>();

        protected Block[] _blocks;

        public override Block GetBlock(int index)
        {
            return _blocks[index];
        }

        public override void SetBlock(int index, Block block)
        {
            _blocks[index] = block;
        }

        public override bool BlocksExsist()
        {
            return _blocks != null;
        }

        public override int GetBlocksLength()
        {
            return _blocks.Length;
        }

        protected virtual void Awake()
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
            _mesh.SetUVs(0, _uv);
            _mesh.SetNormals(_normals);
            _mesh.SetTangents(_tangents);
            _mesh.SetTriangles(_triangles, 0);

            _mesh.RecalculateBounds();
        }

        protected virtual void ClearMeshProperties()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uv.Clear();
            _normals.Clear();
            _tangents.Clear();
        }

        protected virtual void UpdateCollider()
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = this._mesh;
        }

        protected override bool TryUpdateChunkMesh()
        {
            UpdateChunk();
            return true;
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

            IsUpdatingChunk = false;
        }

        public override void AllocateBlocks(int size)
        {
            this._blocks = new Block[Size * Size * Size];
            InitBlocks();
        }

        protected abstract void InitBlocks();

        protected abstract void UpdateMeshProperties();
    }
}
