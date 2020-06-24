using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    public class SimpleWorld : WorldBase
    {
        [SerializeField]
        Vector3Int _ranges = new Vector3Int(1, 1, 1);

        [SerializeField]
        int _chunkSize = 16;

        [SerializeField]
        float _extent = 0.5f;

        [SerializeField]
        ChunkBase _chunkPrefab;

        void Start()
        {
            SpawnChunks();
        }

        void SpawnChunks()
        {
            for (var x = 0; x <= _ranges.x; x++)
            {
                for (var y = 0; y <= _ranges.y; y++)
                {
                    for (var z = 0; z <= _ranges.z; z++)
                    {
                        var cPos = new Vector3Int(x, y, z);
                        Vector3 pos = cPos * _chunkSize; 
                        var chunk = Instantiate(_chunkPrefab, pos, Quaternion.identity);
                        chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";
                        
                        chunk.Initialize(this, _chunkSize, _extent, cPos);
                        _chunks.Add(cPos, chunk);
                    }
                }
            }
        }
    }
}
