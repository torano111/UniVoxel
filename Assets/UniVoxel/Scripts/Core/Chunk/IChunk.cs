using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace UniVoxel.Core
{
    public interface IChunk
    {
        float Extent {get;}
        Vector3Int Position { get; }
        string Name { get; set; }

        void Initialize(IChunkHolder chunkHolder, int chunkSize, float extent, Vector3Int position);
        IReadOnlyReactiveProperty<bool> IsInitialized { get; }

        int GetChunkSize();
        bool TryGetBlock(int x, int y, int z, out Block block);
        bool ContainBlock(int x, int y, int z);
        bool IsSolid(int x, int y, int z);
        bool GetNeedsUpdate();
        void MarkUpdate();
        void SetBlock(int x, int y, int z, Block block);
    }
}
