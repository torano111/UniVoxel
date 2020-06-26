using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UnityStandardAssets.Characters.FirstPerson;

namespace UniVoxel.Core
{
    public class ProceduralWorld : WorldBase
    {

        [SerializeField]
        Vector3Int _ranges = new Vector3Int(3, 1, 3);

        [SerializeField]
        PerlinNoiseChunk _chunkPrefab;

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
            for (var x = 0; x <= 2 * _ranges.x; x++)
            {
                for (var y = 0; y <= 2 * _ranges.y; y++)
                {
                    for (var z = 0; z <= 2 * _ranges.z; z++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _player.transform.position + new Vector3(posX, posY, posZ) * _chunkSize;
                        InitChunk(chunkWorldPos);
                    }
                }
            }
        }

        void InitChunk(Vector3 worldPos)
        {
            var cPos = GetChunkPositionAt(worldPos);
            var chunk = Instantiate(_chunkPrefab, cPos, Quaternion.identity);
            chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";
            chunk.transform.SetParent(this.transform);

            chunk.Initialize(this, _chunkSize, _extent, cPos);
            _chunks.Add(cPos, chunk);
        }
        
        IEnumerator BuildChunks()
        {
            // build chunk at from the player position to distant positions
            for (var x = 0; x <= 2 * _ranges.x; x++)
            {
                for (var y = 0; y <= 2 * _ranges.y; y++)
                {
                    for (var z = 0; z <= 2 * _ranges.z; z++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _player.transform.position + new Vector3(posX, posY, posZ) * _chunkSize;
                        if (_chunks.TryGetValue(GetChunkPositionAt(chunkWorldPos), out var chunk))
                        {
                            chunk.MarkUpdate();
                            // wait for a frame
                            yield return null;
                        }
                        else
                        {
                            Debug.LogAssertion($"chunk not found");
                        }
                    }
                }
            }

            yield return null;

            var playerPos = _player.transform.position;
            _player.transform.position = new Vector3(playerPos.z, playerPos.y + (_ranges.y + 1) * _chunkSize, playerPos.z);
            _player.gameObject.SetActive(true);
        }
    }
}
