using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public class WorldBase : MonoBehaviour, IChunkHolder
    {
        protected Dictionary<Vector3Int, ChunkBase> _chunks = new Dictionary<Vector3Int, ChunkBase>();

        public virtual bool TryGetNeighbourChunk(IChunk chunk, BoxFaceSide neighbourDirection, out IChunk neighbourChunk)
        {
            var neighbourChunkPos = BlockUtility.GetNeighbourPosition(chunk.Position.x, chunk.Position.y, chunk.Position.z, neighbourDirection);

            var result = _chunks.TryGetValue(neighbourChunkPos, out var c);

            neighbourChunk = c as IChunk;

            return result;
        }
    }
}

