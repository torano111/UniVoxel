using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using System;
using System.Linq;
using Random = UnityEngine.Random;

namespace UniVoxel.Core
{
    public class RandomChunk : DynamicChunkBase
    {
        [SerializeField]
        int _spawnBoxRate = 50;

        [SerializeField]
        int _chunkSize = 16;

        [SerializeField]
        float _extent = 0.5f;

        [SerializeField]
        BlockDataScriptableObject _blockDataObject;

        [SerializeField]
        Material _material;

        [SerializeField]
        Vector2 _singleTextureLengths = new Vector2(16f, 16f);

        [SerializeField]
        Vector2 _textureAtlasLengths = new Vector2(256f, 256f);

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

        protected override void InitBlocks()
        {
            var blockTypes = System.Enum.GetValues(typeof(BlockType));

            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        var randomBlockType = (BlockType)Random.Range((int)blockTypes.GetValue(0), blockTypes.Length);
                        var randomNum = Random.Range(0, 100);

                        if (randomNum < _spawnBoxRate)
                        {
                            SetBlock(x, y, z, new Block(randomBlockType));
                        }
                    }
                }
            }
        }

        protected override void UpdateMeshProperties()
        {
            var vertexStartIndex = 0;
            var triangleStartIndex = 0;
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        UpdateChunkMesh(x, y, z, ref vertexStartIndex, ref triangleStartIndex);
                    }
                }
            }
        }

        protected void UpdateChunkMesh(int x, int y, int z, ref int vertexStartIndex, ref int triangleStartIndex)
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
                            vertexStartIndex += VoxelUtility.GetFaceVertexLength();
                            triangleStartIndex += VoxelUtility.GetFaceTriangleLength();
                        }

                        iterateCount++;
                    }
                }
            }
        }
    }
}
