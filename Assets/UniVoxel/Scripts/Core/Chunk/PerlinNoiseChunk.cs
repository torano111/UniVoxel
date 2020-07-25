using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using Unity.Mathematics;

namespace UniVoxel.Core
{
    public class PerlinNoiseChunk : DynamicChunkBase
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

        public Vector2 GetUVCoord00(BlockType blockType, BoxFaceSide side)
        {
            return _blockDataObject.GetUVCoord00(blockType, side, _singleTextureLengths, _textureAtlasLengths);
        }

        public Vector2 GetUVCoord11(BlockType blockType, BoxFaceSide side)
        {
            return _blockDataObject.GetUVCoord11(blockType, side, _singleTextureLengths, _textureAtlasLengths);
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
            if (TryGetBlock(x, y, z, out var block) && block.IsSolid)
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

        void Start()
        {
            _meshRenderer.material = _material;
        }

        protected override void InitBlocks()
        {
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        var blockInfo = ChunkUtility.CalculateBlockInfo(new int3(x, y, z), Noise2D, Noise3D, new int3(Size, Size, Size), Extent, new int3(Position.x, Position.y, Position.z), _perlinNoiseSettings.UseNoise2D, _perlinNoiseSettings.UseNoise3D);
                        if (blockInfo.IsSolid)
                        {
                            SetBlock(x, y, z, new Block(blockInfo.BlockType));
                        }
                    }
                }
            }
        }

        public override bool IsNeighbourSolid(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            if (TryGetBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, out var block))
            {
                return block.IsSolid;
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

                if (_world != null && _world.TryGetNeighbourChunk(this, neighbourDirection, out var neighbourChunk))
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
                    var blockInfo = ChunkUtility.CalculateBlockInfo(new int3(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z), Noise2D, Noise3D, new int3(Size, Size, Size), Extent, new int3(neighbourChunkPos.x, neighbourChunkPos.y, neighbourChunkPos.z), _perlinNoiseSettings.UseNoise2D, _perlinNoiseSettings.UseNoise3D);
                    return blockInfo.IsSolid;
                }

                return false;
            }
        }
    }
}
