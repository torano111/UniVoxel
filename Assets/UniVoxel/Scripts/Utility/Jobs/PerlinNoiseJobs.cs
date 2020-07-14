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
    public struct CalculateMeshWithCounterParallelJob : IJobParallelFor
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
            var nids = BlockUtility.GetNeighbourPosition(blockPos.x, blockPos.y, blockPos.z, side, 1);
            var neighbourBlockIndices = new int3(nids.x, nids.y, nids.z);
            var neighbourBlockLinearIndex = MathUtility.GetLinearIndexFrom3Points(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, size.x, size.z);

            // if the neighbour block is in this chunk
            if (IsBlockInRange(neighbourBlockIndices))
            {
                var neighbourBlock = Blocks[neighbourBlockLinearIndex];
                return neighbourBlock.IsValid;
            }
            // if not, then calculate the noise of the virtual neighbour block
            else
            {
                var diff = neighbourBlockIndices - new int3(blockPos.x, blockPos.y, blockPos.z);
                for (var axis = 0; axis < 3; axis++)
                {
                    if (diff[axis] < 0)
                    {
                        neighbourBlockIndices[axis] = ChunkSize[0][axis] - 1;
                        break;
                    }

                    else if (0 < diff[axis])
                    {
                        neighbourBlockIndices[axis] = 0;
                        break;
                    }
                }

                var neighbourChunkPos = BlockUtility.GetNeighbourPosition(ChunkPosition[0].x, ChunkPosition[0].y, ChunkPosition[0].z, side, ChunkSize[0].x);
                var bInfo = ChunkUtility.CalculateBlockInfo(neighbourBlockIndices, Noise2D[0], Noise3D[0], ChunkSize[0], Extent[0], new int3(neighbourChunkPos.x, neighbourChunkPos.y, neighbourChunkPos.z), UsePerlinNoise[0] != 0, UsePerlinNoise[1] != 0);
                return bInfo.IsSolid;
            }
        }

        bool IsBlockInRange(int3 blockPos)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                if (blockPos[axis] < 0)
                {
                    return false;
                }
                if (ChunkSize[0][axis] <= blockPos[axis])
                {
                    return false;
                }
            }
            return true;
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

    /// <summary>
    /// Calculates solid blocks from blocks in parallel.
    /// Use CalculateSolidBlocksParallelJob, SolidBlockQueueToListJob, and CalculateMeshFromSolidBlockListParallelJob in this order, to calculate mesh properties from blocks.
    /// </summary>
    [BurstCompile]
    public struct CalculateSolidBlocksParallelJob : IJobParallelFor
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
        public NativeQueue<SolidBlockData>.ParallelWriter SolidBlockQueue;


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

            var solidBlock = new SolidBlockData();
            solidBlock.SolidFaceCount = 0;
            solidBlock.BlockIndex = index;

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Front))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Front);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Back))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Back);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Top))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Top);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Bottom))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Bottom);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Right))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Right);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (!IsNeighbourSolid(blockPos, BoxFaceSide.Left))
            {
                solidBlock.SolidFaceCount++;
                var faceSideId = CalculateBlockFacesJobHelper.GetFaceSideIndex(BoxFaceSide.Left);
                solidBlock.SolidFaceMask |= (1 << faceSideId);
            }

            if (solidBlock.SolidFaceCount > 0)
            {
                SolidBlockQueue.Enqueue(solidBlock);
            }
        }

        bool IsNeighbourSolid(int3 blockPos, BoxFaceSide side)
        {
            var size = ChunkSize[0];
            var nids = BlockUtility.GetNeighbourPosition(blockPos.x, blockPos.y, blockPos.z, side, 1);
            var neighbourBlockIndices = new int3(nids.x, nids.y, nids.z);
            var neighbourBlockLinearIndex = MathUtility.GetLinearIndexFrom3Points(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, size.x, size.z);

            // if the neighbour block is in this chunk
            if (IsBlockInRange(neighbourBlockIndices))
            {
                var neighbourBlock = Blocks[neighbourBlockLinearIndex];
                return neighbourBlock.IsValid;
            }
            // if not, then calculate the noise of the virtual neighbour block
            else
            {
                var diff = neighbourBlockIndices - new int3(blockPos.x, blockPos.y, blockPos.z);
                for (var axis = 0; axis < 3; axis++)
                {
                    if (diff[axis] < 0)
                    {
                        neighbourBlockIndices[axis] = ChunkSize[0][axis] - 1;
                        break;
                    }

                    else if (0 < diff[axis])
                    {
                        neighbourBlockIndices[axis] = 0;
                        break;
                    }
                }

                var neighbourChunkPos = BlockUtility.GetNeighbourPosition(ChunkPosition[0].x, ChunkPosition[0].y, ChunkPosition[0].z, side, ChunkSize[0].x);
                var bInfo = ChunkUtility.CalculateBlockInfo(neighbourBlockIndices, Noise2D[0], Noise3D[0], ChunkSize[0], Extent[0], new int3(neighbourChunkPos.x, neighbourChunkPos.y, neighbourChunkPos.z), UsePerlinNoise[0] != 0, UsePerlinNoise[1] != 0);
                return bInfo.IsSolid;
            }
        }

        bool IsBlockInRange(int3 blockPos)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                if (blockPos[axis] < 0)
                {
                    return false;
                }
                if (ChunkSize[0][axis] <= blockPos[axis])
                {
                    return false;
                }
            }
            return true;
        }
    }


    /// <summary>
    /// Moves solid block data from queue to list.
    /// Also, set the length of mesh properties.
    /// Use CalculateSolidBlocksParallelJob, SolidBlockQueueToListJob, and CalculateMeshFromSolidBlockListParallelJob in this order, to calculate mesh properties from blocks.
    /// </summary>
    [BurstCompile]
    public struct SolidBlockQueueToListJob : IJob
    {
        NativeQueue<SolidBlockData> SolidBlockQueue;

        NativeList<SolidBlockData> SolidBlockList;

        NativeList<float3> Vertices;

        NativeList<ushort> Triangles;

        NativeList<float2> UV;

        public void Execute()
        {
            var totalSolidBlockCount = SolidBlockQueue.Count;
            SolidBlockList.Capacity = totalSolidBlockCount;

            var solidFaceCount = 0;
            while (SolidBlockQueue.TryDequeue(out var solidBlock))
            {
                solidBlock.SolidFaceCountBefore = solidFaceCount;
                SolidBlockList.Add(solidBlock);


                solidFaceCount += solidBlock.SolidFaceCount;
            }

            // 4 vertices per face
            Vertices.Length = solidFaceCount * 4;
            UV.Length = solidFaceCount * 4;

            // 2 triangles per face
            Triangles.Length = solidFaceCount * 6;
        }
    }


    /// <summary>
    /// Calculates mesh properties from solid blocks in parallel.
    /// Use CalculateSolidBlocksParallelJob, SolidBlockQueueToListJob, and CalculateMeshFromSolidBlockListParallelJob in this order, to calculate mesh properties from blocks.
    /// </summary>
    [BurstCompile]
    public struct CalculateMeshFromSolidBlockListParallelJob : IJobParallelFor
    {
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
        public NativeList<float3> Vertices;

        [WriteOnly]
        public NativeList<ushort> Triangles;

        [WriteOnly]
        public NativeList<float2> UV0;

        // length should be 1
        [ReadOnly]
        public NativeHashMap<int, BlockData> BlockDatas;

        // length should be 1
        [ReadOnly]
        public NativeArray<float2> SingleTextureLenghts;

        // length should be 1
        [ReadOnly]
        public NativeArray<float2> TextureAtlasLenghts;

        [ReadOnly]
        public NativeList<SolidBlockData> SolidBlocks;

        public void Execute(int index)
        {
            UpdateMeshProperties(index);
        }

        void UpdateMeshProperties(int solidBlockId)
        {
            var solidBlock = SolidBlocks[solidBlockId];
            var blockId = solidBlock.BlockIndex;

            var blockPos = MathUtility.Get3DIndicesFromLinearIndex(blockId, ChunkSize[0].x, ChunkSize[0].z);
            var extent = Extent[0];
            var center = (float3)blockPos * extent * 2.0f;

            var quadCount = solidBlock.SolidFaceCountBefore;
            for (var i = CalculateBlockFacesJobHelper.StartFaceSideIndex; i < CalculateBlockFacesJobHelper.EndFaceSideIndex; i++)
            {
                var mask = 1 << i;
                if ((solidBlock.SolidFaceMask & mask) == mask)
                {
                    var side = CalculateBlockFacesJobHelper.GetBoxFaceSide(i);
                    AddFace(blockId, side, center, extent, quadCount);
                    quadCount++;
                }
            }
        }

        bool HasBlock(int index)
        {
            return 0 <= index && index < Blocks.Length;
        }

        void AddFace(int index, BoxFaceSide side, float3 center, float extent, int quadCount)
        {
            // a quad consists of 2 triangles
            var startTriId = quadCount * 3 * 2;

            // 4 vertices for 2 triangles
            var startVertId = quadCount * 4;

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