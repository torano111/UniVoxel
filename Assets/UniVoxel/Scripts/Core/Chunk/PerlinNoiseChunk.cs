using System.Collections;
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

        Perlin perlin = new Perlin();

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
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var z = 0; z < Size; z++)
                    {
                        Block block = null;

                        if (TryGetBlockType(GetBlockWorldPosition(Position, new Vector3Int(x, y, z)), out var blockType))
                        {
                            block = new Block(blockType);
                        }

                        if (block != null)
                        {
                            SetBlock(x, y, z, block);
                        }
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
            blockType = BlockType.Grass;

            var densityNoise = perlin.GetOctavePerlin3D(worldPos.x * _densityNoiseScaler, worldPos.y * _densityNoiseScaler, worldPos.z * _densityNoiseScaler, _densityNoiseOctaves, _densityNoisePersistence);

            int currentHeight = (int)worldPos.y;

            if (densityNoise <= _densityThreshold)
            {
                return false;
            }
            else
            {
                var heightNoise = perlin.GetOctavePerlin2D(worldPos.x * _heightNoiseScaler, worldPos.z * _heightNoiseScaler, _heightNoiseOctaves, _heightNoisePersistence);
                if (currentHeight <= GetHeightThreshold(_maxStoneLayerHeight, heightNoise))
                {
                    blockType = BlockType.Stone;
                    return true;
                }
                else if (currentHeight < GetHeightThreshold(_maxGroundHeight, heightNoise))
                {
                    blockType = BlockType.Dirt;
                    return true;
                }
                else if (currentHeight == GetHeightThreshold(_maxGroundHeight, heightNoise))
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

        int GetHeightThreshold(float maxHeight, double noise)
        {
            return (int)Mathf.Lerp(_minHeight, maxHeight, (float)noise);
        }

        public override bool IsNeighbourSolid(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            if (ContainBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z))
            {
                return _blocks[neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z] != null;
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

                if (_chunkHolder != null && _chunkHolder.TryGetNeighbourChunk(this, neighbourDirection, out var neighbourChunk))
                {
                    if (!neighbourChunk.ContainBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z))
                    {
                        throw new System.InvalidOperationException($"couldn't find a neighbour block in a neighbour chunk\nchunkPos: x={x}, y={y}, z={z} neighbourPos: x={neighbourBlockIndices.x}, y={neighbourBlockIndices.y}, z={neighbourBlockIndices.z}");
                    }

                    return neighbourChunk.IsSolid(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z);
                }
                // if no chunk found, then calculate noise instead of the neighbour chunk and check if solid
                else
                {
                    var virtualNeighbourBlockWorldPos = GetBlockWorldPosition(neighbourChunkPos, neighbourBlockIndices);

                    return TryGetBlockType(virtualNeighbourBlockWorldPos, out var blockType);
                }
            }
        }
    }
}
