using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public abstract class ChunkBase : MonoBehaviour, IChunk
    {
        public float Extent { get; private set; }
        public Vector3Int Position { get; private set; }
        public string Name { get => gameObject.name; set => gameObject.name = value; }

        public int Size { get; private set; }
        public int GetChunkSize() => Size;

        protected Block[,,] _blocks;
        protected IChunkHolder _chunkHolder;

        bool _needsUpdate;
        public bool NeedsUpdate { get => _needsUpdate && IsInitialized.Value; set => _needsUpdate = value; }

        protected ReactiveProperty<bool> _isInitialized = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsInitialized => _isInitialized;

        public virtual void Initialize(IChunkHolder chunkHolder, int chunkSize, float extent, Vector3Int position)
        {
            this._chunkHolder = chunkHolder;
            this.Size = chunkSize;
            this.Extent = extent;
            this.Position = position;

            this._blocks = new Block[Size, Size, Size];

            _isInitialized.Value = true;
        }

        public virtual bool TryGetBlock(int x, int y, int z, out Block block)
        {
            if (ContainBlock(x, y, z))
            {
                block = _blocks[x, y, z];

                return true;
            }

            block = null;
            return false;
        }

        public virtual bool ContainBlock(int x, int y, int z)
        {
            return IsInitialized.Value && _blocks != null && 0 <= x && x < Size && 0 <= y && y < Size && 0 <= z && z < Size;
        }

        public virtual bool IsSolid(int x, int y, int z)
        {
            return TryGetBlock(x, y, z, out var block) && block != null;
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
                _blocks[x, y, z] = block;
            }
            else
            {
                throw new System.ArgumentOutOfRangeException($"doesn't contain block at x={x}, y={y}, z={z}");
            }
        }

        protected virtual void Awake()
        {
            this.NeedsUpdate = false;
        }

        public virtual bool TryGetNeighbourBlock(int x, int y, int z, BoxFaceSide neighbourDirection, out Block block)
        {
            var neighbourPos = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            return TryGetBlock(neighbourPos.x, neighbourPos.y, neighbourPos.z, out block);
        }

        public virtual bool IsNeighbourSolid(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            var neighbourBlockIndices = BlockUtility.GetNeighbourPosition(x, y, z, neighbourDirection, 1);

            if (ContainBlock(neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z))
            {
                // not solid if the block is null
                return _blocks[neighbourBlockIndices.x, neighbourBlockIndices.y, neighbourBlockIndices.z] != null;
            }
            // check the neighbour block in a neighbour chunk if this chunk doesn't contain neighbour
            else
            {
                if (_chunkHolder != null && _chunkHolder.TryGetNeighbourChunk(this, neighbourDirection, out var neighbourChunk))
                {
                    // get difference between current pos and neighbour pos
                    var diff = neighbourBlockIndices - new Vector3Int(x, y, z);
                    for (var axis = 0; axis < 3; axis++)
                    {
                        // if the difference is negative(should be -1), then the actual position in the neighbour chunk is neighbour size - 1.
                        if (diff[axis] < 0)
                        {
                            neighbourBlockIndices[axis] = neighbourChunk.GetChunkSize() - 1;
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
