using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using UniRx;
using UniRx.Triggers;

namespace UniVoxel.Core
{
    public class ProceduralWorld : WorldBase
    {

        [SerializeField]
        Vector3Int _ranges = new Vector3Int(3, 1, 3);

        [SerializeField]
        ChunkBase _chunkPrefab;

        ChunkPool _chunkPool;

        [SerializeField]
        Transform _playerTransform;

        bool _isInitialized = false;

        Queue<ChunkBase> _chunksToReturn = new Queue<ChunkBase>();
        Queue<Vector3Int> _chunkPositionsToSpawn = new Queue<Vector3Int>();

        [SerializeField]
        int _numChunksToSpawnInAFrame = 4;

        void Start()
        {
            _chunkPool = new ChunkPool(_chunkPrefab, this.transform);

            this.OnDestroyAsObservable()
                .Subscribe(_ =>
                {
                    _chunkPool.Dispose();
                });

            _playerTransform.gameObject.SetActive(false);
            InitChunks();
            StartCoroutine("BuildInitialChunks");
        }

        void InitChunks()
        {
            for (var y = 0; y <= 2 * _ranges.y; y++)
            {
                for (var z = 0; z <= 2 * _ranges.z; z++)
                {
                    for (var x = 0; x <= 2 * _ranges.x; x++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _playerTransform.position + new Vector3(posX, posY, posZ) * ChunkSize;

                        var cPos = GetChunkPositionAt(chunkWorldPos);

                        InitChunk(cPos);
                    }
                }
            }
        }

        ChunkBase InitChunk(Vector3Int cPos)
        {
            if (_chunks.TryGetValue(cPos, out var c))
            {
                return c;
            }

            var chunk = _chunkPool.Rent();
            chunk.transform.position = cPos;
            chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";

            chunk.Initialize(this, ChunkSize, Extent, cPos);
            _chunks.Add(cPos, chunk);

            return chunk;
        }

        IEnumerator BuildInitialChunks()
        {
            // build chunk at from the player position to distant positions
            for (var y = 0; y <= 2 * _ranges.y; y++)
            {
                for (var z = 0; z <= 2 * _ranges.z; z++)
                {
                    for (var x = 0; x <= 2 * _ranges.x; x++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _playerTransform.position + new Vector3(posX, posY, posZ) * ChunkSize;
                        var cPos = GetChunkPositionAt(chunkWorldPos);
                        if (_chunks.TryGetValue(cPos, out var chunk))
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

            IsWorldInitialized = true;
            SpawnPlayer();

            _isInitialized = true;
        }

        void SpawnPlayer()
        {
            var playerPos = _playerTransform.position;
            GetHighestSolidBlockIndices(playerPos, out var chunk, out var blockIndices);
            var spawnPos = new Vector3(playerPos.x, chunk.Position.y + blockIndices.y * chunk.Extent * 2, playerPos.z);
            spawnPos.y += 3f;

            // Debug.Log($"SpawnPos: {spawnPos}, Chunk: {chunk.Name}, BlockIndices: {blockIndices.ToString()}");

            _playerTransform.position = spawnPos;
            _playerTransform.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            CheckActiveChunks();
            ReturnInactiveChunks();
            SpawnChunksInRange();
        }

        bool IsInRange(Vector3 chunkPos0, Vector3 chunkPos1)
        {
            var diffX = Mathf.Abs(chunkPos0.x - chunkPos1.x);
            var diffY = Mathf.Abs(chunkPos0.y - chunkPos1.y);
            var diffZ = Mathf.Abs(chunkPos0.z - chunkPos1.z);

            if (diffX > _ranges.x * ChunkSize || diffY > _ranges.y * ChunkSize || diffZ > _ranges.z * ChunkSize)
            {
                return false;
            }

            return true;
        }

        void CheckActiveChunks()
        {
            if (_chunksToReturn.Count == 0)
            {
                var playerChunkPos = GetChunkPositionAt(_playerTransform.position);
                foreach (var chunk in _chunks.Values)
                {
                    if (!IsInRange(playerChunkPos, chunk.Position))
                    {
                        _chunksToReturn.Enqueue(chunk);
                    }
                }

            }

            if (_chunkPositionsToSpawn.Count == 0)
            {
                for (var y = 0; y <= 2 * _ranges.y; y++)
                {
                    for (var z = 0; z <= 2 * _ranges.z; z++)
                    {
                        for (var x = 0; x <= 2 * _ranges.x; x++)
                        {
                            // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                            var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                            // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                            var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                            // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                            var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                            var chunkWorldPos = _playerTransform.position + new Vector3(posX, posY, posZ) * ChunkSize;
                            var cPos = GetChunkPositionAt(chunkWorldPos);

                            if (!_chunks.ContainsKey(cPos))
                            {
                                _chunkPositionsToSpawn.Enqueue(cPos);
                            }
                        }
                    }
                }
            }

            // Debug.Log($"chunksToDestroy: {_chunksToDestroy.Count}, chunkPositionsToSpawn: {_chunkPositionsToSpawn.Count}");
        }

        void ReturnInactiveChunks()
        {
            while (_chunksToReturn.Count > 0)
            {
                var chunk = _chunksToReturn.Dequeue();

                _chunks.Remove(chunk.Position);
                // Destroy(chunk.gameObject);
                _chunkPool.Return(chunk);
            }
        }

        void SpawnChunksInRange()
        {
            var count = 0;
            while (_chunkPositionsToSpawn.Count > 0 && count < _numChunksToSpawnInAFrame)
            {
                var cPos = _chunkPositionsToSpawn.Dequeue();

                if (!_chunks.ContainsKey(cPos))
                {
                    var chunk = InitChunk(cPos);
                    chunk.MarkUpdate();
                    count++;
                }
            }

            // Debug.Log($"{count} chunks spawned");
        }
    }
}
