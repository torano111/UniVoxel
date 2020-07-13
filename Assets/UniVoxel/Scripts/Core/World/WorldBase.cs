using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UniRx;
using System;

namespace UniVoxel.Core
{
    public abstract class WorldBase : MonoBehaviour
    {
        [SerializeField]
        WorldSettings _worldSettings;

        public WorldData WorldSettingsData => _worldSettings.Data;

        protected int ChunkSize => WorldSettingsData.ChunkSize;

        protected float Extent => WorldSettingsData.Extent;

        ReactiveProperty<bool> _isWorldInitializedRP = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsWorldInitializedRP => _isWorldInitializedRP;
        public bool IsWorldInitialized { get => IsWorldInitializedRP.Value; protected set => _isWorldInitializedRP.Value = value; }

        protected Dictionary<Vector3Int, ChunkBase> _chunks = new Dictionary<Vector3Int, ChunkBase>();

        public Vector3Int MaxChunkPosition
        {
            get 
            {
                Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                foreach (var chunk in _chunks.Keys)
                {
                    max = Vector3Int.Max(max, chunk);
                }

                return max;
            }
        }

        public Vector3Int MinChunkPosition
        {
            get 
            {
                Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var chunk in _chunks.Keys)
                {
                    min = Vector3Int.Min(min, chunk);
                }

                return min;
            }
        }

        public virtual bool TryGetNeighbourChunk(IChunk chunk, BoxFaceSide neighbourDirection, out IChunk neighbourChunk)
        {
            var neighbourChunkPos = BlockUtility.GetNeighbourPosition(chunk.Position.x, chunk.Position.y, chunk.Position.z, neighbourDirection, ChunkSize);

            var result = _chunks.TryGetValue(neighbourChunkPos, out var c);

            neighbourChunk = c as IChunk;

            return IsWorldInitialized && result;
        }

        public virtual bool TryGetChunkAt(Vector3 worldPos, out ChunkBase chunk)
        {
            var pos = GetChunkPositionAt(worldPos);

            if (_chunks.TryGetValue(pos, out chunk))
            {
                // return true if the spedified chunk exists and world has been initialized
                return IsWorldInitialized;
            }

            return false;
        }

        public virtual Vector3Int GetChunkPositionAt(Vector3 worldPos)
        {
            var posX = Mathf.FloorToInt(worldPos.x / ChunkSize) * ChunkSize;
            var posY = Mathf.FloorToInt(worldPos.y / ChunkSize) * ChunkSize;
            var posZ = Mathf.FloorToInt(worldPos.z / ChunkSize) * ChunkSize;

            var pos = new Vector3Int(posX, posY, posZ);
            return pos;
        }

        public void GetHighestSolidBlockIndices(Vector3 worldPos, out ChunkBase chunk, out Vector3Int blockIndices)
        {
            worldPos.y = MaxChunkPosition.y;

            // search through chunks from top to bottom
            while (true)
            {
                var cPos = GetChunkPositionAt(worldPos);

                if (_chunks.TryGetValue(cPos, out var c))
                {
                    var diff = worldPos - cPos;
                    var x = Mathf.FloorToInt(diff.x / (c.Extent * 2f));
                    var z = Mathf.FloorToInt(diff.z / (c.Extent * 2f));

                    // check vertical blocks at (x, z) in the chunk
                    for (var y = c.Size - 1; y >= 0; y--)
                    {
                        if (c.IsSolid(x, y, z))
                        {
                            blockIndices = new Vector3Int(x, y, z);
                            chunk = c;
                            return;
                        }
                    }
                }

                worldPos.y -= ChunkSize;

                if (worldPos.y < MinChunkPosition.y)
                {
                    throw new System.InvalidOperationException("cannot find the chunk");
                }
            }
        }
    }
}

