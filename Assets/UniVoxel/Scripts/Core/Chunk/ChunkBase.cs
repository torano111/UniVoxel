using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniVoxel.Utility;
using System;

namespace UniVoxel.Core
{
    public abstract class ChunkBase : MonoBehaviour
    {
        public float Extent { get; protected set; }
        public Vector3Int Position { get; protected set; }
        public string Name { get => gameObject.name; set => gameObject.name = value; }

        public int Size { get; protected set; }

        protected Block[] _blocks;
        protected WorldBase _world;

        bool _needsUpdate;
        public bool NeedsUpdate { get => _needsUpdate && IsInitialized.Value; set => _needsUpdate = value; }

        protected ReactiveProperty<bool> _isInitialized = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsInitialized => _isInitialized;

        ReactiveProperty<bool> _isUpdatingChunkRP = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsUpdatingChunkRP { get => _isUpdatingChunkRP; }

        public bool IsUpdatingChunk { get => IsUpdatingChunkRP.Value; protected set => _isUpdatingChunkRP.Value = value; }

        public virtual void Initialize(WorldBase world, int chunkSize, float extent, Vector3Int position)
        {
            this._world = world;
            this.Size = chunkSize;
            this.Extent = extent;
            this.Position = position;

            this._blocks = new Block[Size * Size * Size];

            _isInitialized.Value = true;
        }

        public virtual bool TryGetBlock(int x, int y, int z, out Block block)
        {
            if (ContainBlock(x, y, z))
            {
                var index = MathUtility.GetLinearIndexFrom3Points(x, y, z, Size, Size);
                block = _blocks[index];

                return true;
            }

            block = default(Block);
            return false;
        }

        
        public virtual Vector3Int GetBlockIndicesAt(Vector3 worldPos)
        {
            worldPos -= Position;
            var x = Mathf.RoundToInt(worldPos.x / (Extent * 2f));
            var y = Mathf.RoundToInt(worldPos.y / (Extent * 2f));
            var z = Mathf.RoundToInt(worldPos.z / (Extent * 2f));

            return new Vector3Int(x, y, z);
        }
        
        public bool IsBlockOnEdge(int x, int y, int z)
        {
            return x == 0 || x == Size - 1 || y == 0 || y == Size - 1 || z == 0 || z == Size - 1;
        }

        public virtual bool ContainBlock(int x, int y, int z)
        {
            return IsInitialized.Value && _blocks != null && 0 <= x && x < Size && 0 <= y && y < Size && 0 <= z && z < Size;
        }

        public virtual bool IsSolid(int x, int y, int z)
        {
            return TryGetBlock(x, y, z, out var block) && block.IsValid;
        }

        public virtual bool GetNeedsUpdate()
        {
            return NeedsUpdate;
        }

        public virtual void MarkUpdate()
        {
            this.NeedsUpdate = true;
        }

        public virtual void SetBlock(int x, int y, int z, Block block)
        {
            if (ContainBlock(x, y, z))
            {
                var index = MathUtility.GetLinearIndexFrom3Points(x, y, z, Size, Size);
                _blocks[index] = block;
            }
            else
            {
                throw new System.ArgumentOutOfRangeException($"doesn't contain block at x={x}, y={y}, z={z}");
            }
        }

        public virtual bool TryGetNeighbourBlock(int x, int y, int z, BoxFaceSide neighbourDirection, out Block block)
        {
            var neighbourPos = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            return TryGetBlock(neighbourPos.x, neighbourPos.y, neighbourPos.z, out block);
        }

        public virtual bool IsNeighbourSolid(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            if (TryGetBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z, out var block))
            {
                // not solid if the block is null
                return block.IsValid;
            }
            // check the neighbour block in a neighbour chunk if this chunk doesn't contain neighbour
            else
            {
                if (_world != null && _world.TryGetNeighbourChunk(this, neighbourDirection, out var neighbourChunk))
                {
                    // get difference between current pos and neighbour pos
                    var diff = neighbourBlockIndices - new Vector3Int(x, y, z);
                    for (var axis = 0; axis < 3; axis++)
                    {
                        // if the difference is negative(should be -1), then the actual position in the neighbour chunk is neighbour size - 1.
                        if (diff[axis] < 0)
                        {
                            neighbourBlockIndices[axis] = neighbourChunk.Size - 1;
                            break;
                        }

                        // if the difference is positive(should be +1), then the actual position in the neighbour chunk is 0.
                        else if (0 < diff[axis])
                        {
                            neighbourBlockIndices[axis] = 0;
                            break;
                        }
                    }

                    // check if the correct position is given
                    if (!neighbourChunk.ContainBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z))
                    {
                        throw new System.InvalidOperationException($"couldn't find a neighbour block in a neighbour chunk\nchunkPos: x={x}, y={y}, z={z} neighbourPos: x={neighbourBlockIndices.x}, y={neighbourBlockIndices.y}, z={neighbourBlockIndices.z}");
                    }

                    return neighbourChunk.IsSolid(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z);
                }

                // if neither chunk holder nor neighbour chunk found, then just return false
                return false;
            }
        }
    }
}
