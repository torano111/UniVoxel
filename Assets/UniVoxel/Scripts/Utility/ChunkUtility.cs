using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    public struct BlockInfo
    {
        public BlockType BlockType;
        public bool IsSolid;
    }

    public static class ChunkUtility
    {
        public static BlockInfo CalculateBlockInfo(int blockIndex, PerlinNoise2DData noise2DData, PerlinNoise3DData noise3DData, int3 chunkSize, float extent, int3 chunkPosition, bool useNoise2D, bool useNoise3D)
        {
            var blockPos = MathUtility.Get3DIndicesFromLinearIndex(blockIndex, chunkSize.x, chunkSize.z);
            return CalculateBlockInfo(blockPos, noise2DData, noise3DData, chunkSize, extent, chunkPosition, useNoise2D, useNoise3D);
        }
        
        public static BlockInfo CalculateBlockInfo(int3 blockPos, PerlinNoise2DData noise2DData, PerlinNoise3DData noise3DData, int3 chunkSize, float extent, int3 chunkPosition, bool useNoise2D, bool useNoise3D)
        {
            var result = new BlockInfo();
            bool isSolid;
            BlockType blockType = default(BlockType);

            var BlockWorldPosition = GetBlockWorldPosition(chunkPosition, blockPos, extent);

            var densityNoise = CalculateNoise3D(noise3DData, BlockWorldPosition);
            int currentHeight = (int)BlockWorldPosition.y;

            // if use noise 3d and the noise value is less than or equal to the threshold
            if (useNoise3D && densityNoise <= noise3DData.DensityThreshold)
            {
                isSolid = false;
            }
            else
            {
                // if not use noise 2d
                if (!useNoise2D)
                {
                    isSolid = true;
                }
                else
                {
                    var heightNoise = CalculateNoise2D(noise2DData, BlockWorldPosition);

                    if (currentHeight <= GetHeightThreshold(noise2DData, noise2DData.MaxStoneLayerHeight, heightNoise))
                    {
                        blockType = BlockType.Stone;
                        isSolid = true;
                    }
                    else if (currentHeight < GetHeightThreshold(noise2DData, noise2DData.MaxGroundHeight, heightNoise))
                    {
                        blockType = BlockType.Dirt;
                        isSolid = true;
                    }
                    else if (currentHeight == GetHeightThreshold(noise2DData, noise2DData.MaxGroundHeight, heightNoise))
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

            result.BlockType = blockType;
            result.IsSolid = isSolid;
            return result;
        }

        public static float3 GetBlockWorldPosition(int3 chunkWorldPos, float3 blockIndices, float extent)
        {
            return chunkWorldPos + (float3)blockIndices * extent * 2f;
        }

        public static double CalculateNoise2D(PerlinNoise2DData noise2DData, float3 worldPos)
        {
            return JobPerlin.GetOctavePerlin2D(new float2(worldPos.x, worldPos.z) * noise2DData.HeightNoiseScaler, noise2DData.HeightNoiseOctaves, noise2DData.HeightNoisePersistence);
        }

        public static double CalculateNoise3D(PerlinNoise3DData noise3DData, float3 worldPos)
        {
            return JobPerlin.GetOctavePerlin3D(worldPos * noise3DData.DensityNoiseScaler, noise3DData.DensityNoiseOctaves, noise3DData.DensityNoisePersistence);
        }

        public static int GetHeightThreshold(PerlinNoise2DData noise2DData, float maxHeight, double noise)
        {
            return (int)math.lerp(noise2DData.MinHeight, maxHeight, noise);
        }
    }
}
