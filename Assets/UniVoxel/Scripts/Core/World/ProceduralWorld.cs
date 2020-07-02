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

        bool _isInitialized = false;

        Queue<ChunkBase> _chunksToDestroy = new Queue<ChunkBase>();
        Queue<Vector3Int> _chunkPositionsToSpawn = new Queue<Vector3Int>();

        [SerializeField]
        int _numChunksToSpawnInAFrame = 4;

        void GetHighestSolidBlockIndices(float worldPosX, float worldPosZ, out ChunkBase chunk, out Vector3Int blockIndices)
        {
            var worldPosY = _player.transform.position.y + _ranges.y * ChunkSize;
            var worldPos = new Vector3(worldPosX, worldPosY, worldPosZ);

            while (true)
            {
                var cPos = GetChunkPositionAt(worldPos);

                if (_chunks.TryGetValue(cPos, out var c))
                {
                    var diff = worldPos - cPos;
                    var x = Mathf.FloorToInt(diff.x / (c.Extent * 2f));
                    var z = Mathf.FloorToInt(diff.z / (c.Extent * 2f));

                    // check vertical blocks at (x, z) in the chunk
                    for (var y = c.Size - 1; y >= 0; y--)
                    {
                        if (c.IsSolid(x, y, z))
                        {
                            blockIndices = new Vector3Int(x, y, z);
                            chunk = c;
                            return;
                        }
                    }
                }

                worldPos.y -= ChunkSize;

                if (worldPos.y < _player.transform.position.y - _ranges.y * ChunkSize)
                {
                    throw new System.InvalidOperationException("cannot find the chunk");
                }
            }
        }

        void Start()
        {
            _player.gameObject.SetActive(false);
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

                        var chunkWorldPos = _player.transform.position + new Vector3(posX, posY, posZ) * ChunkSize;
                        InitChunk(chunkWorldPos);
                    }
                }
            }
        }

        ChunkBase InitChunk(Vector3 worldPos)
        {
            var cPos = GetChunkPositionAt(worldPos);
            var chunk = Instantiate(_chunkPrefab, cPos, Quaternion.identity);
            chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";
            chunk.transform.SetParent(this.transform);

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

                        var chunkWorldPos = _player.transform.position + new Vector3(posX, posY, posZ) * ChunkSize;
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

            SpawnPlayer();

            _isInitialized = true;
        }

        void SpawnPlayer()
        {
            var playerPos = _player.transform.position;
            GetHighestSolidBlockIndices(playerPos.x, playerPos.z, out var chunk, out var blockIndices);
            var spawnPos = new Vector3(playerPos.x, chunk.Position.y + blockIndices.y * chunk.Extent * 2, playerPos.z);
            spawnPos.y += 3f;

            Debug.Log($"SpawnPos: {spawnPos}, Chunk: {chunk.Name}, BlockIndices: {blockIndices.ToString()}");

            _player.transform.position = spawnPos;
            _player.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            CheckActiveChunks();
            RemoveInactiveChunks();
            SpawnChunksInRange();

            // if (_chunkPositionsToSpawn.Count > 0)
            // {
            //     StartCoroutine("SpawnChunksInRangeCoroutine");
            // }
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
            if (_chunksToDestroy.Count == 0)
            {
                var playerChunkPos = GetChunkPositionAt(_player.transform.position);
                foreach (var chunk in _chunks.Values)
                {
                    if (!IsInRange(playerChunkPos, chunk.Position))
                    {
                        _chunksToDestroy.Enqueue(chunk);
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

                            var chunkWorldPos = _player.transform.position + new Vector3(posX, posY, posZ) * ChunkSize;
                            var cPos = GetChunkPositionAt(chunkWorldPos);

                            if (!_chunks.ContainsKey(cPos))
                            {
                                _chunkPositionsToSpawn.Enqueue(cPos);
                            }
                        }
                    }
                }
            }

            Debug.Log($"chunksToDestroy: {_chunksToDestroy.Count}, chunkPositionsToSpawn: {_chunkPositionsToSpawn.Count}");
        }

        void RemoveInactiveChunks()
        {
            while (_chunksToDestroy.Count > 0)
            {
                var chunk = _chunksToDestroy.Dequeue();

                _chunks.Remove(chunk.Position);
                Destroy(chunk.gameObject);
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

            Debug.Log($"{count} chunks spawned");
        }

        IEnumerator SpawnChunksInRangeCoroutine()
        {
            var count = 0;
            while (_chunkPositionsToSpawn.Count > 0)
            {
                if (count < _numChunksToSpawnInAFrame)
                {
                    yield return null;
                    count = 0;
                }

                var cPos = _chunkPositionsToSpawn.Dequeue();

                if (!_chunks.ContainsKey(cPos))
                {
                    var chunk = InitChunk(cPos);
                    chunk.MarkUpdate();
                    count++;
                }
            }

            Debug.Log($"{count} chunks spawned");
        }
    }
}
