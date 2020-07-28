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
    public class LowMemoryJobPerlinNoiseChunk : JobChunkBase
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
        protected CalculateSolidBlocksParallelJob CalculateSolidBlocksJob;
        protected SolidBlockQueueToListJob SolidBlockQueueToListJob;
        protected CalculateMeshFromSolidBlockListParallelJob CalculateMeshJob;

        protected NativeArray<PerlinNoise2DData> NativeNoise2D;
        protected NativeArray<PerlinNoise3DData> NativeNoise3D;
        protected NativeArray<int> NativeUsePerlinNoise;
        protected NativeArray<int3> NativeChunkPosition;
        protected NativeArray<float> NativeExtent;
        protected NativeArray<int3> NativeChunkSize;
        public NativeHashMap<int, BlockData> NativeBlockDatas;
        public NativeArray<float2> NativeSingleTextureLenghts;
        public NativeArray<float2> NativeTextureAtlasLenghts;

        protected NativeQueue<SolidBlockData> NativeSolidBlockQueue;

        protected NativeList<SolidBlockData> NativeSolidBlockList;

        protected NativeList<float3> NativeVertices;

        protected NativeList<ushort> NativeTriangles;

        protected NativeList<float2> NativeUV;


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
            // update chunk values
            NativeChunkPosition[0] = new int3(Position.x, Position.y, Position.z);
            NativeExtent[0] = Extent;
            NativeChunkSize[0] = new int3(Size, Size, Size);

            InitBlocksJob = new CalculateBlocksParallelJob()
            {
                Noise2D = NativeNoise2D,
                Noise3D = NativeNoise3D,
                UsePerlinNoise = NativeUsePerlinNoise,
                ChunkPosition = NativeChunkPosition,
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = NativeBlocks
            };

            dependency = InitBlocksJob.Schedule(GetBlocksLength(), 0, dependency);

            return dependency;
        }

        protected override void OnCompleteInitializeBlocksJob()
        {
            DisposeOnCompleteInitializeBlocksJob();
        }

        protected virtual void DisposeOnCompleteInitializeBlocksJob()
        {
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

            NativeSolidBlockQueue = new NativeQueue<SolidBlockData>(Allocator.Persistent);
            NativeSolidBlockList = new NativeList<SolidBlockData>(Allocator.Persistent);
            NativeVertices = new NativeList<float3>(Allocator.Persistent);
            NativeTriangles = new NativeList<ushort>(Allocator.Persistent);
            NativeUV = new NativeList<float2>(Allocator.Persistent);
        }

        protected virtual void DisposePersistentNativeArrays()
        {
            if (NativeBlocks.IsCreated)
            {
                NativeBlocks.Dispose();
            }

            if (NativeNoise2D.IsCreated)
            {
                NativeNoise2D.Dispose();
            }

            if (NativeNoise3D.IsCreated)
            {
                NativeNoise3D.Dispose();
            }

            if (NativeUsePerlinNoise.IsCreated)
            {
                NativeUsePerlinNoise.Dispose();
            }

            if (NativeChunkPosition.IsCreated)
            {
                NativeChunkPosition.Dispose();
            }

            if (NativeExtent.IsCreated)
            {
                NativeExtent.Dispose();
            }

            if (NativeChunkSize.IsCreated)
            {
                NativeChunkSize.Dispose();
            }

            if (NativeBlockDatas.IsCreated)
            {
                NativeBlockDatas.Dispose();
            }

            if (NativeSingleTextureLenghts.IsCreated)
            {
                NativeSingleTextureLenghts.Dispose();
            }

            if (NativeTextureAtlasLenghts.IsCreated)
            {
                NativeTextureAtlasLenghts.Dispose();
            }

            if (NativeSolidBlockQueue.IsCreated)
            {
                NativeSolidBlockQueue.Dispose();
            }

            if (NativeSolidBlockList.IsCreated)
            {
                NativeSolidBlockList.Dispose();
            }

            if (NativeVertices.IsCreated)
            {
                NativeVertices.Dispose();
            }

            if (NativeTriangles.IsCreated)
            {
                NativeTriangles.Dispose();
            }

            if (NativeUV.IsCreated)
            {
                NativeUV.Dispose();
            }
        }

        protected override void DisposeOnDestroy()
        {
            DisposePersistentNativeArrays();
            DisposeTempJobArrays();
        }

        protected virtual void DisposeTempJobArrays()
        {
            DisposeOnCompleteInitializeBlocksJob();
            DisposeOnCompleteUpdateMeshPropertiesJob();
        }

        protected override JobHandle ScheduleUpdateMeshPropertiesJob(JobHandle dependency)
        {
            CalculateSolidBlocksJob = new CalculateSolidBlocksParallelJob()
            {
                Noise2D = NativeNoise2D,
                Noise3D = NativeNoise3D,
                UsePerlinNoise = NativeUsePerlinNoise,
                ChunkPosition = NativeChunkPosition,
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = NativeBlocks,
                FrontNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Front).NativeBlocks,
                BackNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Back).NativeBlocks,
                TopNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Top).NativeBlocks,
                BottomNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Bottom).NativeBlocks,
                RightNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Right).NativeBlocks,
                LeftNeighbourBlocks = GetNeighbourChunk(BoxFaceSide.Left).NativeBlocks,
                SolidBlockQueue = NativeSolidBlockQueue.AsParallelWriter(),
            };

            dependency = CalculateSolidBlocksJob.Schedule(GetBlocksLength(), 0, dependency);

            SolidBlockQueueToListJob = new SolidBlockQueueToListJob()
            {
                SolidBlockQueue = NativeSolidBlockQueue,
                SolidBlockList = NativeSolidBlockList,
                Vertices = NativeVertices,
                Triangles = NativeTriangles,
                UV = NativeUV,
            };

            dependency = SolidBlockQueueToListJob.Schedule(dependency);

            CalculateMeshJob = new CalculateMeshFromSolidBlockListParallelJob()
            {
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = CalculateSolidBlocksJob.Blocks,
                Vertices = NativeVertices.AsDeferredJobArray(),
                Triangles = NativeTriangles.AsDeferredJobArray(),
                UV0 = NativeUV.AsDeferredJobArray(),
                BlockDatas = NativeBlockDatas,
                SingleTextureLenghts = NativeSingleTextureLenghts,
                TextureAtlasLenghts = NativeTextureAtlasLenghts,
                SolidBlocks = NativeSolidBlockList,
            };

            dependency = CalculateMeshJob.Schedule(NativeSolidBlockList, 0, dependency);

            return dependency;
        }

        protected override void OnCompleteUpdateMeshPropertiesJob()
        {
            // debug
            // var jobInfo = $"Mesh Info of LowMemoryJobPerlinNoiseChunk={Name}\n";
            // var quadCount = 0;
            // for (var i = 0; i < NativeSolidBlockList.Length; i++)
            // {
            //     quadCount += NativeSolidBlockList[i].SolidFaceCount;
            // }

            // jobInfo += $"SolidBlockList={NativeSolidBlockList.Length}, QuadCount={quadCount}";
            
            // if (NativeSolidBlockList.Length > 0)
            // {
            //     var lastQuadCountBefore = NativeSolidBlockList[NativeSolidBlockList.Length - 1].SolidFaceCountBefore;
            //     var lastQuadCount = NativeSolidBlockList[NativeSolidBlockList.Length - 1].SolidFaceCount;
            //     jobInfo += $", lastQuadCountBefore+lastQuadCount={lastQuadCountBefore + lastQuadCount}";
            // }

            // jobInfo += $"\nVertices={NativeVertices.Length}, Triangles={NativeTriangles.Length}, UV={NativeUV.Length}";
            // Debug.Log(jobInfo);
            
            ClearNativeLists();

            DisposeOnCompleteUpdateMeshPropertiesJob();
        }

        protected void ClearNativeLists()
        {
            NativeSolidBlockList.Clear();
            NativeVertices.Clear();
            NativeTriangles.Clear();
            NativeUV.Clear();
        }

        void DisposeOnCompleteUpdateMeshPropertiesJob()
        {
            
        }

        protected override NativeArray<float3> GetVertices(ref int startId, ref int count)
        {
            startId = 0;
            count = NativeVertices.Length;
            return NativeVertices;
        }

        protected override NativeArray<ushort> GetTriangles(ref int startId, ref int count)
        {
            startId = 0;
            count = NativeTriangles.Length;
            return NativeTriangles;
        }

        protected override NativeArray<float2> GetUV(ref int startId, ref int count)
        {
            startId = 0;
            count = NativeUV.Length;
            return NativeUV;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearNativeLists();
            DisposeTempJobArrays();
            // Debug.Log($"Disable chunk={Name}");
        }
    }
}
