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
        Vector3Int _minChunkPos= new Vector3Int(0, 0, 0);

        [SerializeField]
        Vector3Int _maxChunkPos = new Vector3Int(1, 1, 1);

        [SerializeField]
        ChunkBase _chunkPrefab;

        [SerializeField]
        FirstPersonController _player;

        void Start()
        {
            _player.gameObject.SetActive(false);

            InitChunks();
            StartCoroutine("BuildChunks");
        }

        void InitChunks()
        {
            for (var x = _minChunkPos.x; x <= _maxChunkPos.x; x++)
            {
                for (var y = _minChunkPos.y; y <= _maxChunkPos.y; y++)
                {
                    for (var z = _minChunkPos.z; z <= _maxChunkPos.z; z++)
                    {
                        var cPos = new Vector3Int(x, y, z);
                        cPos *= _chunkSize;
                        var chunk = Instantiate(_chunkPrefab, cPos, Quaternion.identity);
                        chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";
                        chunk.transform.SetParent(this.transform);

                        chunk.Initialize(this, _chunkSize, _extent, cPos);
                        _chunks.Add(cPos, chunk);
                    }
                }
            }
        }

        IEnumerator BuildChunks()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.MarkUpdate();
                yield return null;
            }

            yield return null;
            
            var playerPos = _player.transform.position;
            _player.transform.position = new Vector3(playerPos.z, playerPos.y + (_maxChunkPos.y + 1) * _chunkSize, playerPos.z);
            _player.gameObject.SetActive(true);
        }
    }
}
