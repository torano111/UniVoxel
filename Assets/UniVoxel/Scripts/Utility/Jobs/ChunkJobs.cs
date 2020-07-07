using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UniVoxel.Utility;
using UniVoxel.Core;

namespace UniVoxel.Utility.Jobs
{
    [BurstCompile]
    public struct CalculateBlocksParallelJob : IJobParallelFor
    {
        // length should be 1
        [ReadOnly]
        public NativeArray<PerlinNoise2DData> Noise2D;

        // length should be 1
        [ReadOnly]
        public NativeArray<PerlinNoise3DData> Noise3D;

        // length should be 2. the first one is for Noise2DData, and the second for Noise3DData.
        // false if 0, and true if not 0
        [ReadOnly]
        public NativeArray<int> UsePerlinNoise;

        // length should be 1
        [ReadOnly]
        public NativeArray<int3> ChunkPosition;

        // length should be 1
        [ReadOnly]
        public NativeArray<float> Extent;

        // length should be 1
        [ReadOnly]
        public NativeArray<int3> ChunkSize;

        public NativeArray<Block> Blocks;

        [ReadOnly]
        public NativeArray<int> Permutation;

        public void Execute(int index)
        {
            bool isSolid;
            BlockType blockType = default(BlockType);

            var BlockWorldPosition = GetBlockWorldPosition(ChunkPosition[0], (float3)MathUtility.Get3DIndicesFromLinearIndex(index, ChunkSize[0].x, ChunkSize[0].z));

            var densityNoise = CalculateNoise3D(BlockWorldPosition);
            int currentHeight = (int)BlockWorldPosition.y;

            // if use noise 3d and the noise value is less than or equal to the threshold
            if (UsePerlinNoise[1] != 0 && densityNoise <= Noise3D[0].DensityThreshold)
            {
                isSolid = false;
            }
            else
            {
                // if not use noise 2d
                if (UsePerlinNoise[0] == 0)
                {
                    isSolid = true;
                }
                else
                {
                    var heightNoise = CalculateNoise2D(BlockWorldPosition);

                    if (currentHeight <= GetHeightThreshold(Noise2D[0].MaxStoneLayerHeight, heightNoise))
                    {
                        blockType = BlockType.Stone;
                        isSolid = true;
                    }
                    else if (currentHeight < GetHeightThreshold(Noise2D[0].MaxGroundHeight, heightNoise))
                    {
                        blockType = BlockType.Dirt;
                        isSolid = true;
                    }
                    else if (currentHeight == GetHeightThreshold(Noise2D[0].MaxGroundHeight, heightNoise))
                    {
                        blockType = BlockType.Grass;
                        isSolid = true;
                    }
                    else
                    {
                        isSolid = false;
                    }
                }

            }

            var b = new Block(blockType);
            b.IsValid = isSolid;
            Blocks[index] = b;
        }

        float3 GetBlockWorldPosition(int3 chunkWorldPos, float3 blockIndices)
        {
            return chunkWorldPos + (float3)blockIndices * Extent[0] * 2f;
        }

        public double CalculateNoise2D(float3 worldPos)
        {
            return JobPerlin.GetOctavePerlin2D(new float2(worldPos.x, worldPos.z) * Noise2D[0].HeightNoiseScaler, Noise2D[0].HeightNoiseOctaves, Noise2D[0].HeightNoisePersistence, Permutation);
        }

        public double CalculateNoise3D(float3 worldPos)
        {
            return JobPerlin.GetOctavePerlin3D(worldPos * Noise3D[0].DensityNoiseScaler, Noise3D[0].DensityNoiseOctaves, Noise3D[0].DensityNoisePersistence, Permutation);
        }

        int GetHeightThreshold(float maxHeight, double noise)
        {
            return (int)math.lerp(Noise2D[0].MinHeight, maxHeight, noise);
        }
    }

    [BurstCompile]
    public struct CalculateMeshPropertiesParallelJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            throw new NotImplementedException();
        }
    }
}
