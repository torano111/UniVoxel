using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public class WorldBase : MonoBehaviour
    {
        [SerializeField]
        WorldSettings _worldSettings;

        public WorldData WorldSettingsData => _worldSettings.Data;
        
        protected int ChunkSize => WorldSettingsData.ChunkSize;

        protected float Extent => WorldSettingsData.Extent;

        protected Dictionary<Vector3Int, ChunkBase> _chunks = new Dictionary<Vector3Int, ChunkBase>();

        public virtual bool TryGetNeighbourChunk(IChunk chunk, BoxFaceSide neighbourDirection, out IChunk neighbourChunk)
        {
            var neighbourChunkPos = BlockUtility.GetNeighbourPosition(chunk.Position.x, chunk.Position.y, chunk.Position.z, neighbourDirection, ChunkSize);

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
            var posX = Mathf.FloorToInt(worldPos.x / ChunkSize) * ChunkSize;
            var posY = Mathf.FloorToInt(worldPos.y / ChunkSize) * ChunkSize;
            var posZ = Mathf.FloorToInt(worldPos.z / ChunkSize) * ChunkSize;
            
            var pos = new Vector3Int(posX, posY, posZ);
            return pos;
        }
    }
}

