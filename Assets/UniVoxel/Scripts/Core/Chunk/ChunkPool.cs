using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Toolkit;

namespace UniVoxel.Core
{
    public class ChunkPool : ObjectPool<ChunkBase>
    {
        Transform _parent;
        ChunkBase _chunkPrefab;

        public ChunkPool(ChunkBase chunkPrefab, Transform parent = null)
        {
            this._chunkPrefab = chunkPrefab;
            this._parent = parent;
        }

        protected override ChunkBase CreateInstance()
        {
            var chunk = GameObject.Instantiate(_chunkPrefab);
            chunk.transform.SetParent(_parent);

            return chunk;
        }
    }
}
