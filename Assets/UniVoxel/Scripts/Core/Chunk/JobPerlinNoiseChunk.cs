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

        [SerializeField]
        bool _accurateSolidCheck = true;

        protected CalculateBlocksParallelJob CalculateBlocksJob;
        protected NativeArray<PerlinNoise2DData> NativeNoise2D;
        protected NativeArray<PerlinNoise3DData> NativeNoise3D;
        protected NativeArray<int> NativeUsePerlinNoise;
        protected NativeArray<int3> NativeChunkPosition;
        protected NativeArray<float> NativeExtent;
        protected NativeArray<int3> NativeChunkSize;
        protected NativeArray<Block> NativeBlocks;

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

        protected override void UpdateMeshProperties()
        {
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        UpdateChunkMesh(x, y, z);
                    }
                }
            }
        }

        protected void UpdateChunkMesh(int x, int y, int z)
        {
            if (TryGetBlock(x, y, z, out var block) && block.IsValid)
            {
                var nonSolidNeighbourCount = 0;
                var iterateCount = 0;

                // 1 if NOT solid 
                var notSolidFrag = 0;

                foreach (BoxFaceSide side in FaceSides)
                {
                    if (!IsNeighbourSolid(x, y, z, side))
                    {
                        nonSolidNeighbourCount++;
                        notSolidFrag = notSolidFrag | (1 << iterateCount);
                    }

                    iterateCount++;
                }

                if (nonSolidNeighbourCount > 0)
                {
                    iterateCount = 0;
                    foreach (BoxFaceSide side in FaceSides)
                    {
                        var isNotSolid = notSolidFrag & (1 << iterateCount);
                        if (isNotSolid > 0)
                        {
                            var center = new Vector3(x, y, z) * Extent * 2.0f;
                            VoxelUtility.AddMeshForBoxFace(side, center, Extent, _vertices, _triangles, _uv, GetUVCoord00(block.BlockType, side), GetUVCoord11(block.BlockType, side), _normals, _tangents);
                        }

                        iterateCount++;
                    }
                }
            }
        }

        Vector3 GetBlockWorldPosition(Vector3 chunkWorldPos, Vector3Int blockIndices)
        {
            return chunkWorldPos + (Vector3)blockIndices * Extent * 2f;
        }

        public bool TryGetBlockType(Vector3 worldPos, out BlockType blockType)
        {
            blockType = default(BlockType);

            var densityNoise = CalculateNoise3D(worldPos);
            int currentHeight = (int)worldPos.y;

            if (_perlinNoiseSettings.UseNoise3D && densityNoise <= Noise3D.DensityThreshold)
            {
                return false;
            }
            else
            {
                if (!_perlinNoiseSettings.UseNoise2D)
                {
                    return true;
                }

                var heightNoise = CalculateNoise2D(worldPos);

                if (currentHeight <= GetHeightThreshold(Noise2D.MaxStoneLayerHeight, heightNoise))
                {
                    blockType = BlockType.Stone;
                    return true;
                }
                else if (currentHeight < GetHeightThreshold(Noise2D.MaxGroundHeight, heightNoise))
                {
                    blockType = BlockType.Dirt;
                    return true;
                }
                else if (currentHeight == GetHeightThreshold(Noise2D.MaxGroundHeight, heightNoise))
                {
                    blockType = BlockType.Grass;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public double CalculateNoise2D(Vector3 worldPos)
        {
            // make the coordinates positive since calculating Perlin Noise may not work correctly with negative values.
            return Perlin.GetOctavePerlin2D(worldPos.x * Noise2D.HeightNoiseScaler, worldPos.z * Noise2D.HeightNoiseScaler, Noise2D.HeightNoiseOctaves, Noise2D.HeightNoisePersistence);
        }

        public double CalculateNoise3D(Vector3 worldPos)
        {
            // make the coordinates positive since calculating Perlin Noise may not work correctly with negative values.
            return Perlin.GetOctavePerlin3D(worldPos.x * Noise3D.DensityNoiseScaler, worldPos.y * Noise3D.DensityNoiseScaler, worldPos.z * Noise3D.DensityNoiseScaler, Noise3D.DensityNoiseOctaves, Noise3D.DensityNoisePersistence);
        }

        int GetHeightThreshold(float maxHeight, double noise)
        {
            return (int)Mathf.Lerp(Noise2D.MinHeight, maxHeight, (float)noise);
        }

        public override bool IsNeighbourSolid(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            if (TryGetBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, out var block))
            {
                return block.IsValid;
            }
            else
            {
                var neighbourChunkPos = BlockUtility.GetNeighbourPosition(this.Position.x, this.Position.y, this.Position.z, neighbourDirection, Size);

                var diff = neighbourBlockIndices - new Vector3Int(x, y, z);
                for (var axis = 0; axis < 3; axis++)
                {
                    if (diff[axis] < 0)
                    {
                        neighbourBlockIndices[axis] = Size - 1;
                        break;
                    }

                    else if (0 < diff[axis])
                    {
                        neighbourBlockIndices[axis] = 0;
                        break;
                    }
                }

                if (_chunkHolder != null && _chunkHolder.TryGetNeighbourChunk(this, neighbourDirection, out var neighbourChunk) && neighbourChunk.IsInitialized.Value)
                {
                    if (!neighbourChunk.ContainBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z))
                    {
                        throw new System.InvalidOperationException($"couldn't find a neighbour block in a neighbour chunk\nchunk pos: x={Position.x}, y={Position.y}, z={Position.z}, block position: x={x}, y={y}, z={z} neighbour block pos: x={neighbourBlockIndices.x}, y={neighbourBlockIndices.y}, z={neighbourBlockIndices.z}");
                    }

                    return neighbourChunk.IsSolid(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z);
                }
                // if no chunk found, then calculate noise instead of the neighbour chunk and check if solid
                else if (_accurateSolidCheck)
                {
                    var virtualNeighbourBlockWorldPos = GetBlockWorldPosition(neighbourChunkPos, neighbourBlockIndices);

                    return TryGetBlockType(virtualNeighbourBlockWorldPos, out var blockType);
                }

                return false;
            }
        }

        protected override JobHandle ScheduleInitializeBlocksJob(JobHandle dependency = default)
        {
            NativeBlocks = new NativeArray<Block>(_blocks, Allocator.TempJob);

            CalculateBlocksJob = new CalculateBlocksParallelJob()
            {
                Noise2D = NativeNoise2D,
                Noise3D = NativeNoise3D,
                UsePerlinNoise = NativeUsePerlinNoise,
                ChunkPosition = NativeChunkPosition,
                Extent = NativeExtent,
                ChunkSize = NativeChunkSize,
                Blocks = NativeBlocks,
                Permutation = PerlinNoisePermutationTableHolder.Instance.Permutation,
            };

            dependency = CalculateBlocksJob.Schedule(_blocks.Length, 0, dependency);

            return dependency;
        }

        protected override void OnCompleteInitializeBlocksJob()
        {
            NativeBlocks.CopyTo(_blocks);
            DisposeOnCompleteInitializeBlocksJob();
        }

        protected virtual void DisposeOnCompleteInitializeBlocksJob()
        {
            if (NativeBlocks.IsCreated)
            {
                NativeBlocks.Dispose();
            }
        }

        protected override void InitializePersistentNativeArrays()
        {
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
        }

        protected virtual void DisposePersistentNativeArrays()
        {
            NativeNoise2D.Dispose();
            NativeNoise3D.Dispose();
            NativeUsePerlinNoise.Dispose();
            NativeChunkPosition.Dispose();
            NativeExtent.Dispose();
            NativeChunkSize.Dispose();
        }

        protected override void DisposeOnDestroy()
        {
            DisposePersistentNativeArrays();
            DisposeOnCompleteInitializeBlocksJob();
        }
    }
}
