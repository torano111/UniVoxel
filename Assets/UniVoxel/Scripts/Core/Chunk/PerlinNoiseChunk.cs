﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public class PerlinNoiseChunk : DynamicChunkBase
    {
        [SerializeField]
        int _maxGroundHeight = 20;

        public int MaxGroundHeight => _maxGroundHeight;

        [SerializeField]
        int _maxStoneLayerHeight = 15;

        [SerializeField]
        int _minHeight = 0;

        [SerializeField]
        double _densityThreshold = 0.5;

        [SerializeField]
        float _heightNoiseScaler = 0.1f;

        [SerializeField]
        int _heightNoiseOctaves = 4;

        [SerializeField]
        double _heightNoisePersistence = 0.5;

        [SerializeField]
        float _densityNoiseScaler = 0.1f;

        [SerializeField]
        int _densityNoiseOctaves = 4;

        [SerializeField]
        double _densityNoisePersistence = 0.5;

        [SerializeField]
        Material _material;

        [SerializeField]
        BlockDataScriptableObject _blockDataObject;

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
            if (TryGetBlock(x, y, z, out var block) && block != null)
            {
                var nonSolidNeighbourCount = 0;
                var iterateCount = 0;

                // 1 if NOT solid 
                var notSolidFrag = 0;

                foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
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
                    VoxelUtility.ReserveMeshForFaces(nonSolidNeighbourCount, ref _vertices, ref _triangles, ref _uv, ref _normals, ref _tangents);

                    iterateCount = 0;
                    foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
                    {
                        var isNotSolid = notSolidFrag & (1 << iterateCount);
                        if (isNotSolid > 0)
                        {
                            var center = new Vector3(x, y, z) * Extent * 2.0f;
                            VoxelUtility.AddMeshForBoxFace(side, center, Extent, _vertices, _triangles, _uv, GetUVCoord00(block.BlockType, side), GetUVCoord11(block.BlockType, side), _normals, _tangents, vertexStartIndex, triangleStartIndex);
                            vertexStartIndex += VoxelUtility.GetFaceVertexLength();
                            triangleStartIndex += VoxelUtility.GetFaceTriangleLength();
                        }

                        iterateCount++;
                    }
                }
            }
        }

        void Start()
        {
            _meshRenderer.material = _material;

            InitBlocks();
        }

        void InitBlocks()
        {
            Perlin perlin = new Perlin();

            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        Block block = null;
                        var worldPos = gameObject.transform.position + new Vector3(x, y, z) * Extent * 2;

                        var densityNoise = perlin.GetOctavePerlin3D(worldPos.x * _densityNoiseScaler, worldPos.y * _densityNoiseScaler, worldPos.z * _densityNoiseScaler, _densityNoiseOctaves, _densityNoisePersistence);

                        int currentHeight = (int)worldPos.y;

                        if (densityNoise <= _densityThreshold)
                        {
                            block = null;
                        }
                        else
                        {
                            var heightNoise = perlin.GetOctavePerlin2D(worldPos.x * _heightNoiseScaler, worldPos.z * _heightNoiseScaler, _heightNoiseOctaves, _heightNoisePersistence);
                            if (currentHeight <= GetHeightThreshold(_maxStoneLayerHeight, heightNoise))
                            {
                                block = new Block(BlockType.Stone);
                            }
                            else if (currentHeight < GetHeightThreshold(_maxGroundHeight, heightNoise))
                            {
                                block = new Block(BlockType.Dirt);
                            }
                            else if (currentHeight == GetHeightThreshold(_maxGroundHeight, heightNoise))
                            {
                                block = new Block(BlockType.Grass);
                            }
                            else
                            {
                                block = null;
                            }
                        }

                        if (block != null)
                        {
                            SetBlock(x, y, z, block);
                        }
                    }
                }
            }

            MarkUpdate();
        }

        int GetHeightThreshold(float maxHeight, double noise)
        {
            return (int)Mathf.Lerp(_minHeight, maxHeight, (float)noise);
        }
    }
}
