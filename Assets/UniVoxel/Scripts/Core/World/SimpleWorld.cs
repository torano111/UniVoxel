using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UnityStandardAssets.Characters.FirstPerson;

namespace UniVoxel.Core
{
    public class SimpleWorld : WorldBase
    {
        [SerializeField]
        Vector3Int _ranges = new Vector3Int(1, 1, 1);

        [SerializeField]
        ChunkBase _chunkPrefab;

        [SerializeField]
        FirstPersonController _player;

        [SerializeField]
        Vector3 _playerSpawnPoint = new Vector3(0, 0, 0);

        void Start()
        {
            SpawnChunks();
            SpawnPlayer();
        }

        void SpawnChunks()
        {
            if (_player == null)
            {
                return;
            }

            for (var x = 0; x <= _ranges.x; x++)
            {
                for (var y = 0; y <= _ranges.y; y++)
                {
                    for (var z = 0; z <= _ranges.z; z++)
                    {
                        var cPos = new Vector3Int(x, y, z);
                        cPos *= _chunkSize;
                        var chunk = Instantiate(_chunkPrefab, cPos, Quaternion.identity);
                        chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";

                        chunk.Initialize(this, _chunkSize, _extent, cPos);
                        _chunks.Add(cPos, chunk);
                    }
                }
            }
        }

        void SpawnPlayer()
        {
            if (TryGetChunkAt(new Vector3(_playerSpawnPoint.x, _ranges.y * _chunkSize, _playerSpawnPoint.z), out var chunk))
            {
                if (chunk is PerlinNoiseChunk perlinChunk)
                {
                    var height = (float)perlinChunk.MaxGroundHeight + 1f;
                    var spawnPos = new Vector3(_playerSpawnPoint.x, height, _playerSpawnPoint.z);

                    var player = Instantiate(_player, spawnPos, Quaternion.identity);
                }
                else
                {
                    Debug.Log("PerlinNoiseChunk has to be used to spawn a player on a chunk");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Chunk not found at spawn point");
                return;
            }
        }
    }
}
