using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UniVoxel.Utility.Jobs;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace UniVoxel.Core
{
    public class JobPerlinNoiseChunk : JobChunkBase
    {
        [SerializeField]
        PerlinNoiseSettings _perlinNoiseSettings;

        protected PerlinNoise2DData Noise2D => _perlinNoiseSettings.Noise2D;
        protected PerlinNoise3DData Noise3D => _perlinNoiseSettings.Noise3D;

        [SerializeField]
        Material _material;

        [SerializeField]
        BlockDataScriptableObject _blockDataObject;

        [SerializeField]
        Vector2 _singleTextureLengths = new Vector2(16f, 16f);

        [SerializeField]
        Vector2 _textureAtlasLengths = new Vector2(256f, 256f);

        protected CalculateBlocksParallelJob InitBlocksJob;
        
        protected CalculateMeshPropertiesParallelJob MeshPropertiesJob;
        
        protected NativeArray<PerlinNoise2DData> NativeNoise2D;
        protected NativeArray<PerlinNoise3DData> NativeNoise3D;
        protected NativeArray<int> NativeUsePerlinNoise;
        protected NativeArray<int3> NativeChunkPosition;
        protected NativeArray<float> NativeExtent;
        protected NativeArray<int3> NativeChunkSize;
        public NativeHashMap<int, BlockData> NativeBlockDatas;
        public NativeArray<float2> NativeSingleTextureLenghts;
        public NativeArray<float2> NativeTextureAtlasLenghts;

        protected NativeArray<float3> NativeVertices;

        protected NativeArray<ushort> NativeTriangles;

        protected NativeArray<float2> NativeUV;

        protected NativeCounter Counter;

        public Vector2 GetUVCoord00(BlockType blockType, BoxFaceSide side)
        {
            return _blockDataObject.GetUVCoord00(blockType, side, _singleTextureLengths, _textureAtlasLengths);
        }

        public Vector2 GetUVCoord11(BlockType blockType, BoxFaceSide side)
        {
            return _blockDataObject.GetUVCoord11(blockType, side, _singleTextureLengths, _textureAtlasLengths);
        }

        void Start()
        {
            _meshRenderer.material = _material;
        }

        protected override JobHandle ScheduleInitializeBlocksJob(JobHandle dependency = default)
        {
            InitBlocksJob = new CalculateBlocksParallelJob()
            {
                Noise2D = NativeNoise2D,
                Noise3D = NativeNoise3D,
                UsePerlinNoise = NativeUsePerlinNoise,
                ChunkPosition = NativeChunkPosition,
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = new NativeArray<Block>(_blocks, Allocator.TempJob),
            };

            dependency = InitBlocksJob.Schedule(_blocks.Length, 0, dependency);

            return dependency;
        }

        protected override void OnCompleteInitializeBlocksJob()
        {
            InitBlocksJob.Blocks.CopyTo(_blocks);
            DisposeOnCompleteInitializeBlocksJob();
        }

        protected virtual void DisposeOnCompleteInitializeBlocksJob()
        {
            if (InitBlocksJob.Blocks.IsCreated)
            {
                InitBlocksJob.Blocks.Dispose();
            }
        }

        protected override void InitializePersistentNativeArrays()
        {
            // NativeArrays used to initialize blocks

            NativeNoise2D = new NativeArray<PerlinNoise2DData>(1, Allocator.Persistent);
            NativeNoise3D = new NativeArray<PerlinNoise3DData>(1, Allocator.Persistent);
            NativeUsePerlinNoise = new NativeArray<int>(2, Allocator.Persistent);
            NativeChunkPosition = new NativeArray<int3>(1, Allocator.Persistent);
            NativeExtent = new NativeArray<float>(1, Allocator.Persistent);
            NativeChunkSize = new NativeArray<int3>(1, Allocator.Persistent);

            NativeNoise2D[0] = Noise2D;
            NativeNoise3D[0] = Noise3D;

            NativeUsePerlinNoise[0] = _perlinNoiseSettings.UseNoise2D ? 1 : 0;
            NativeUsePerlinNoise[1] = _perlinNoiseSettings.UseNoise3D ? 1 : 0;

            NativeChunkPosition[0] = new int3(Position.x, Position.y, Position.z);
            NativeExtent[0] = Extent;
            NativeChunkSize[0] = new int3(Size, Size, Size);

            // NativeArrays used to calculate Mesh

            var blockTypes = System.Enum.GetValues(typeof(BlockType));
            NativeBlockDatas = new NativeHashMap<int, BlockData>(blockTypes.Length, Allocator.Persistent);
            
            foreach (BlockType blockType in blockTypes)
            {
                if (_blockDataObject.TryGetBlockData(blockType, out var data))
                {
                    NativeBlockDatas.Add((int)blockType, data);
                }
                else
                {
                    Debug.LogAssertion($"BlockData of {blockType.ToString()} not found");
                }
            }

            NativeSingleTextureLenghts = new NativeArray<float2>(1, Allocator.Persistent);
            NativeTextureAtlasLenghts = new NativeArray<float2>(1, Allocator.Persistent);

            NativeSingleTextureLenghts[0] = _singleTextureLengths;
            NativeTextureAtlasLenghts[0] = _textureAtlasLengths;

            NativeVertices = new NativeArray<float3>(Size * Size * Size * 4 * 6, Allocator.Persistent);
            NativeTriangles = new NativeArray<ushort>(Size * Size * Size * 6 * 6, Allocator.Persistent);
            NativeUV = new NativeArray<float2>(NativeVertices.Length, Allocator.Persistent);
            Counter = new NativeCounter(Allocator.Persistent);
        }

        protected virtual void DisposePersistentNativeArrays()
        {
            NativeNoise2D.Dispose();
            NativeNoise3D.Dispose();
            NativeUsePerlinNoise.Dispose();
            NativeChunkPosition.Dispose();
            NativeExtent.Dispose();
            NativeChunkSize.Dispose();

            NativeBlockDatas.Dispose();
            NativeSingleTextureLenghts.Dispose();
            NativeTextureAtlasLenghts.Dispose();

            NativeVertices.Dispose();
            NativeTriangles.Dispose();
            NativeUV.Dispose();
            Counter.Dispose();
        }

        protected override void DisposeOnDestroy()
        {
            JobHandle.Complete();
            DisposePersistentNativeArrays();
            DisposeOnCompleteInitializeBlocksJob();
            DisposeOnCompleteUpdateMeshPropertiesJob();
        }

        protected override JobHandle ScheduleUpdateMeshPropertiesJob(JobHandle dependency)
        {
            Counter.Count = 0;

            MeshPropertiesJob = new CalculateMeshPropertiesParallelJob()
            {
                Noise2D = NativeNoise2D,
                Noise3D = NativeNoise3D,
                UsePerlinNoise = NativeUsePerlinNoise,
                ChunkPosition = NativeChunkPosition,
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = new NativeArray<Block>(_blocks, Allocator.TempJob),
                Counter = Counter,
                Vertices = NativeVertices,
                Triangles = NativeTriangles,
                UV0 = NativeUV,
                BlockDatas = NativeBlockDatas,
                SingleTextureLenghts = NativeSingleTextureLenghts,
                TextureAtlasLenghts = NativeTextureAtlasLenghts,
            };

            dependency = MeshPropertiesJob.Schedule(_blocks.Length, 0, dependency);

            return dependency;
        }

        protected override void OnCompleteUpdateMeshPropertiesJob()
        {
            DisposeOnCompleteUpdateMeshPropertiesJob();
        }

        void DisposeOnCompleteUpdateMeshPropertiesJob()
        {
            if (MeshPropertiesJob.Blocks.IsCreated)
            {   
                MeshPropertiesJob.Blocks.Dispose();
            }
        }

        protected override NativeArray<float3> GetVertices(ref int startId, ref int count)
        {
            startId= 0;
            count = Counter.Count * 4;
            return NativeVertices;
        }

        protected override NativeArray<ushort> GetTriangles(ref int startId, ref int count)
        {
            startId= 0;
            count = Counter.Count * 6;
            return NativeTriangles;
        }

        protected override NativeArray<float2> GetUV(ref int startId, ref int count)
        {
            startId= 0;
            count = Counter.Count * 4;
            return NativeUV;
        }
    }
}
