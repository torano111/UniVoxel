using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using System;
using UniRx;
using UnityEngine.Profiling;

namespace UniVoxel.Core
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public abstract class JobChunkBase : ChunkBase
    {
        protected static BoxFaceSide[] FaceSides = new BoxFaceSide[] { BoxFaceSide.Front, BoxFaceSide.Back, BoxFaceSide.Top, BoxFaceSide.Bottom, BoxFaceSide.Right, BoxFaceSide.Left };
        protected MeshFilter _meshFilter;
        protected MeshRenderer _meshRenderer;
        protected MeshCollider _meshCollider;

        protected Mesh _mesh;

        public JobHandle InitJobHandle { get; protected set; }
        public JobHandle UpdateMeshJobHandle { get; protected set; }

        public void AddDependencyToInitJobHandle(JobHandle dependency)
        {
            JobHandle.CombineDependencies(InitJobHandle, dependency);
        }

        public void AddDependencyToUpdateMeshJobHandle(JobHandle dependency)
        {
            JobHandle.CombineDependencies(UpdateMeshJobHandle, dependency);
        }

        protected bool IsUpdateMeshPropertiesJobCompleted = true;

        public override void Initialize(WorldBase world, int chunkSize, float extent, Vector3Int position)
        {
            _isInitialized.Value = false;

            this._world = world;
            this.Size = chunkSize;
            this.Extent = extent;
            this.Position = position;

            var blockslength = Size * Size * Size;

            if (_blocks == null || _blocks.Length != blockslength)
            {
                this._blocks = new Block[blockslength];
                DisposeOnDestroy();
                InitializePersistentNativeArrays();
            }

            // Debug.Log($"chunk({Name}): schedule init blocks job");
            AddDependencyToInitJobHandle(UpdateMeshJobHandle);
            InitJobHandle = ScheduleInitializeBlocksJob(InitJobHandle);
        }

        protected virtual void InitializePersistentNativeArrays() { }

        protected abstract JobHandle ScheduleInitializeBlocksJob(JobHandle dependency = new JobHandle());

        protected virtual void CompleteInitializeBlocksJob()
        {
            InitJobHandle.Complete();
            OnCompleteInitializeBlocksJob();
            _isInitialized.Value = true;
        }

        protected abstract void DisposeOnDestroy();

        protected abstract void OnCompleteInitializeBlocksJob();

        protected virtual void OnDestroy()
        {
            InitJobHandle.Complete();
            UpdateMeshJobHandle.Complete();

            DisposeOnDestroy();
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
            _mesh.name = "ChunkMesh";
            _mesh.MarkDynamic();
            _meshFilter.mesh = this._mesh;

            this._meshFilter.mesh = this._mesh;
        }

        protected virtual void UpdateRenderer()
        {
            _mesh.Clear();

            var startId = 0;
            var count = 0;
            var vertices = GetVertices(ref startId, ref count);
            if (count <= 0)
            {
                return;
            }

            _mesh.SetVertices(vertices, startId, count);

            var uv = GetUV(ref startId, ref count);
            _mesh.SetUVs(0, uv, startId, count);

            var triangles = GetTriangles(ref startId, ref count);
            _mesh.SetIndices(triangles, startId, count, MeshTopology.Triangles, 0, true);

            _mesh.RecalculateNormals();

            _meshRenderer.enabled = true;
        }

        protected virtual void UpdateCollider()
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = this._mesh;
        }

        protected virtual void UpdateChunk(bool updatesCollider = true)
        {
            UpdateRenderer();

            if (updatesCollider)
            {
                UpdateCollider();
            }
        }

        protected abstract JobHandle ScheduleUpdateMeshPropertiesJob(JobHandle dependency);

        protected abstract void OnCompleteUpdateMeshPropertiesJob();

        protected void CompleteUpdateMeshPropertiesJob()
        {
            UpdateMeshJobHandle.Complete();
            UpdateChunk(true);

            OnCompleteUpdateMeshPropertiesJob();
        }

        protected virtual void Update()
        {
            if (!IsInitialized.Value)
            {
                CompleteInitializeBlocksJob();
            }

            if (IsUpdatingChunk)
            {
                CompleteUpdateMeshPropertiesJob();
                IsUpdatingChunk = false;
                this.NeedsUpdate = false;
            }
        }

        public bool TryScheduleUpdateMeshJob()
        {
            if (NeedsUpdate && !IsUpdatingChunk && CheckNeighbourChunks())
            {
                IsUpdatingChunk = true;
                TryAddNeighbourDependencies();
                AddDependencyToUpdateMeshJobHandle(InitJobHandle);
                UpdateMeshJobHandle = ScheduleUpdateMeshPropertiesJob(UpdateMeshJobHandle);
                return true;
            }

            return false;
        }

        protected virtual bool TryAddNeighbourDependencies()
        {    
            return TryAddNeighbourDependency(BoxFaceSide.Front) && TryAddNeighbourDependency(BoxFaceSide.Back) && TryAddNeighbourDependency(BoxFaceSide.Top) && TryAddNeighbourDependency(BoxFaceSide.Bottom) && TryAddNeighbourDependency(BoxFaceSide.Right) && TryAddNeighbourDependency(BoxFaceSide.Left);
        }

        protected virtual bool TryAddNeighbourDependency(BoxFaceSide side)
        {
            if ( _world.TryGetNeighbourChunk(this, side, out var chunk) && chunk is JobChunkBase jobChunk)
            {
                AddDependencyToUpdateMeshJobHandle(jobChunk.InitJobHandle);
                return true;
            }

            return false;
        }

        protected virtual bool CheckNeighbourChunks()
        {
            return CheckNeighbourChunk(BoxFaceSide.Front) && CheckNeighbourChunk(BoxFaceSide.Back) && CheckNeighbourChunk(BoxFaceSide.Top) && CheckNeighbourChunk(BoxFaceSide.Bottom) && CheckNeighbourChunk(BoxFaceSide.Right) && CheckNeighbourChunk(BoxFaceSide.Left);
        }

        protected virtual bool CheckNeighbourChunk(BoxFaceSide side)
        {
            return _world.TryGetNeighbourChunk(this, side, out var chunk);
        }

        protected abstract NativeArray<float3> GetVertices(ref int startId, ref int count);

        protected abstract NativeArray<ushort> GetTriangles(ref int startId, ref int count);

        protected abstract NativeArray<float2> GetUV(ref int startId, ref int count);

        protected virtual void OnEnable()
        {
            this.NeedsUpdate = false;
            _meshRenderer.enabled = false;
        }

        protected virtual void OnDisable()
        {
            InitJobHandle.Complete();
            UpdateMeshJobHandle.Complete();
        }

        public virtual string GetDebugInfo()
        {
            var debugInfo = $"Debugging Chunk={Name}\n";
            debugInfo += $"InitJobHandle.IsCompleted={InitJobHandle.IsCompleted}\n";
            debugInfo += $"UpdateMeshJobHandle.IsCompleted={UpdateMeshJobHandle.IsCompleted}\n";
            debugInfo += $"IsInitialized={IsInitialized.Value}\n";
            debugInfo += $"NeedsUpdate={NeedsUpdate}\n";
            debugInfo += $"IsUpdatingChunk={IsUpdatingChunk}\n";

            return debugInfo;
        }
    }
}