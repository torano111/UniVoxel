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

        public void Execute(int index)
        {
            var bInfo = ChunkUtility.CalculateBlockInfo(index, Noise2D[0], Noise3D[0], ChunkSize[0], Extent[0], ChunkPosition[0], UsePerlinNoise[0] != 0, UsePerlinNoise[1] != 0);

            var b = new Block(bInfo.BlockType);
            b.IsValid = bInfo.IsSolid;
            Blocks[index] = b;
        }
    }

    [BurstCompile]
    public struct CalculateMeshPropertiesParallelJob : IJobParallelFor
    {
        ///// Noise Calculation /////

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

        //////////

        [ReadOnly]
        public NativeArray<Block> Blocks;

        [WriteOnly]
        public NativeCounter.Concurrent Counter;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> Vertices;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<ushort> Triangles;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float2> UV0;

        // length should be 1
        [ReadOnly]
        public NativeHashMap<int, BlockData> BlockDatas;

        // length should be 1
        [ReadOnly]
        public NativeArray<float2> SingleTextureLenghts;

        // length should be 1
        [ReadOnly]
        public NativeArray<float2> TextureAtlasLenghts;

        public void Execute(int index)
        {
            var block = Blocks[index];

            if (!block.IsValid)
            {
                return;
            }

            UpdateMeshProperties(index);
        }

        void UpdateMeshProperties(int index)
        {
            var blockPos = MathUtility.Get3DIndicesFromLinearIndex(index, ChunkSize[0].x, ChunkSize[0].z);
            var extent = Extent[0];
            var center = (float3)blockPos * extent * 2.0f;

            int quadCount;
            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Front))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Front, center, extent, quadCount);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Back))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Back, center, extent, quadCount);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Top))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Top, center, extent, quadCount);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Bottom))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Bottom, center, extent, quadCount);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Right))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Right, center, extent, quadCount);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Left))
            {
                quadCount = Counter.Increment();
                AddFace(index, BoxFaceSide.Left, center, extent, quadCount);
            }
        }

        bool IsNeighbourSolid(int3 blockPos, BoxFaceSide side)
        {
            var size = ChunkSize[0];
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(blockPos.x, blockPos.y, blockPos.z, side, 1);
            var neighbourBlockLinearIndex = MathUtility.GetLinearIndexFrom3Points(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, size.x, size.z);

            // if the neighbour block is in this chunk
            if (HasBlock(neighbourBlockLinearIndex))
            {
                var neighbourBlock = Blocks[neighbourBlockLinearIndex];
                return neighbourBlock.IsValid;
            }
            // if not, then calculate the noise of the virtual neighbour block
            else
            {
                var bInfo = ChunkUtility.CalculateBlockInfo(neighbourBlockLinearIndex, Noise2D[0], Noise3D[0], ChunkSize[0], Extent[0], ChunkPosition[0], UsePerlinNoise[0] != 0, UsePerlinNoise[1] != 0);
                return bInfo.IsSolid;
            }
        }

        bool HasBlock(int index)
        {
            return 0 <= index && index < Blocks.Length;
        }

        void AddFace(int index, BoxFaceSide side, float3 center, float extent, int quadCount)
        {
            // a quad consists of 2 triangles
            var startTriId = (quadCount - 1) * 3 * 2;

            // 4 vertices for 2 triangles
            var startVertId = (quadCount - 1) * 4;

            var block = Blocks[index];

            AddVertices(side, center, extent, startVertId);
            AddTriangles(side, startVertId, startTriId);
            AddUV(side, GetUVCoord00(index, block.BlockType, side), GetUVCoord11(index, block.BlockType, side), startVertId);
        }

        void AddVertices(BoxFaceSide faceSide, float3 center, float extent, int startIndex)
        {
            switch (faceSide)
            {
                case BoxFaceSide.Front:
                    Vertices[startIndex] = center + new float3(extent, -extent, extent);
                    Vertices[startIndex + 1] = center + new float3(-extent, -extent, extent);
                    Vertices[startIndex + 2] = center + new float3(extent, extent, extent);
                    Vertices[startIndex + 3] = center + new float3(-extent, extent, extent);
                    break;
                case BoxFaceSide.Back:
                    Vertices[startIndex] = center + new float3(-extent, -extent, -extent);
                    Vertices[startIndex + 1] = center + new float3(extent, -extent, -extent);
                    Vertices[startIndex + 2] = center + new float3(-extent, extent, -extent);
                    Vertices[startIndex + 3] = center + new float3(extent, extent, -extent);
                    break;
                case BoxFaceSide.Top:
                    Vertices[startIndex] = center + new float3(-extent, extent, -extent);
                    Vertices[startIndex + 1] = center + new float3(extent, extent, -extent);
                    Vertices[startIndex + 2] = center + new float3(-extent, extent, extent);
                    Vertices[startIndex + 3] = center + new float3(extent, extent, extent);
                    break;
                case BoxFaceSide.Bottom:
                    Vertices[startIndex] = center + new float3(extent, -extent, -extent);
                    Vertices[startIndex + 1] = center + new float3(-extent, -extent, -extent);
                    Vertices[startIndex + 2] = center + new float3(extent, -extent, extent);
                    Vertices[startIndex + 3] = center + new float3(-extent, -extent, extent);
                    break;
                case BoxFaceSide.Right:
                    Vertices[startIndex] = center + new float3(extent, -extent, -extent);
                    Vertices[startIndex + 1] = center + new float3(extent, -extent, extent);
                    Vertices[startIndex + 2] = center + new float3(extent, extent, -extent);
                    Vertices[startIndex + 3] = center + new float3(extent, extent, extent);
                    break;
                case BoxFaceSide.Left:
                    Vertices[startIndex] = center + new float3(-extent, -extent, extent);
                    Vertices[startIndex + 1] = center + new float3(-extent, -extent, -extent);
                    Vertices[startIndex + 2] = center + new float3(-extent, extent, extent);
                    Vertices[startIndex + 3] = center + new float3(-extent, extent, -extent);
                    break;
            }
        }

        void AddTriangles(BoxFaceSide faceSide, int vertexStartIndex, int triangleStartIndex)
        {
            switch (faceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    // up side triangle
                    Triangles[triangleStartIndex] = (ushort)(vertexStartIndex);
                    Triangles[triangleStartIndex + 1] = (ushort)(vertexStartIndex + 2);
                    Triangles[triangleStartIndex + 2] = (ushort)(vertexStartIndex + 1);

                    // down side triangle
                    Triangles[triangleStartIndex + 3] = (ushort)(vertexStartIndex + 2);
                    Triangles[triangleStartIndex + 4] = (ushort)(vertexStartIndex + 3);
                    Triangles[triangleStartIndex + 5] = (ushort)(vertexStartIndex + 1);
                    break;
            }
        }

        void AddUV(BoxFaceSide faceSide, Vector2 UVCoord00, Vector2 UVCoord11, int startIndex)
        {
            switch (faceSide)
            {
                case BoxFaceSide.Front:
                case BoxFaceSide.Back:
                case BoxFaceSide.Top:
                case BoxFaceSide.Bottom:
                case BoxFaceSide.Right:
                case BoxFaceSide.Left:
                    UV0[startIndex] = new float2(UVCoord00.x, UVCoord00.y);
                    UV0[startIndex + 1] = new float2(UVCoord11.x, UVCoord00.y);
                    UV0[startIndex + 2] = new float2(UVCoord00.x, UVCoord11.y);
                    UV0[startIndex + 3] = new float2(UVCoord11.x, UVCoord11.y);
                    break;
            }
        }

        float2 GetUVCoord00(int index, BlockType blockType, BoxFaceSide side)
        {
            return GetUVCoord00(index, blockType, side, SingleTextureLenghts[0], TextureAtlasLenghts[0]);
        }

        float2 GetUVCoord11(int index, BlockType blockType, BoxFaceSide side)
        {
            return GetUVCoord11(index, blockType, side, SingleTextureLenghts[0], TextureAtlasLenghts[0]);
        }

        float2 GetUVCoord00(int index, BlockType blockType, BoxFaceSide side, float2 singleTextureLengths, float2 textureAtlasLengths)
        {
            var block = Blocks[index];
            float2 result = float2.zero;
            if (HasBlock(index) && BlockDatas.TryGetValue((int)block.BlockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                result = BlockUtility.GetUV00FromTextureAtlas(texAtlasPos, singleTextureLengths, textureAtlasLengths);
            }

            return result;
        }

        float2 GetUVCoord11(int index, BlockType blockType, BoxFaceSide side, float2 singleTextureLengths, float2 textureAtlasLengths)
        {
            var block = Blocks[index];
            float2 result = float2.zero;
            if (HasBlock(index) && BlockDatas.TryGetValue((int)block.BlockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                result = BlockUtility.GetUV11FromTextureAtlas(texAtlasPos, singleTextureLengths, textureAtlasLengths);
            }

            return result;
        }
    }
}