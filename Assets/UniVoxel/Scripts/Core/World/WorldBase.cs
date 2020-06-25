using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public class WorldBase : MonoBehaviour, IChunkHolder
    {
        [SerializeField]
        protected int _chunkSize = 16;

        [SerializeField]
        protected float _extent = 0.5f;

        protected Dictionary<Vector3Int, ChunkBase> _chunks = new Dictionary<Vector3Int, ChunkBase>();

        public virtual bool TryGetNeighbourChunk(IChunk chunk, BoxFaceSide neighbourDirection, out IChunk neighbourChunk)
        {
            var neighbourChunkPos = BlockUtility.GetNeighbourPosition(chunk.Position.x, chunk.Position.y, chunk.Position.z, neighbourDirection);

            var result = _chunks.TryGetValue(neighbourChunkPos, out var c);

            neighbourChunk = c as IChunk;

            return result;
        }

        public virtual bool TryGetChunkAt(Vector3 worldPos, out ChunkBase chunk)
        {
            var pos = GetChunkPositionAt(worldPos);

            if (_chunks.TryGetValue(pos, out chunk))
            {
                return true;
            }

            return false;
        }

        public virtual Vector3Int GetChunkPositionAt(Vector3 worldPos)
        {
            var posX = Mathf.FloorToInt(worldPos.x / _chunkSize) * _chunkSize;
            var posY = Mathf.FloorToInt(worldPos.y / _chunkSize) * _chunkSize;
            var posZ = Mathf.FloorToInt(worldPos.z / _chunkSize) * _chunkSize;
            
            var pos = new Vector3Int(posX, posY, posZ);
            return pos;
        }
    }
}

