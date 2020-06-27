using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniVoxel.Core
{
    public interface IChunkHolder
    {
        WorldData WorldSettingsData { get; }

        bool TryGetNeighbourChunk(IChunk chunk, BoxFaceSide neighbourDirection, out IChunk neighbourChunk);
        bool TryGetChunkAt(Vector3 worldPos, out ChunkBase chunk);
        Vector3Int GetChunkPositionAt(Vector3 worldPos);
    }
}
