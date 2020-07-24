using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using UniRx;
using UniRx.Triggers;
using Unity.Jobs;

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

        Queue<ChunkBase> _chunksToReturn = new Queue<ChunkBase>();
        Queue<Vector3Int> _chunkPositionsToSpawn = new Queue<Vector3Int>();
        Queue<ChunkBase> _chunksToSpawn = new Queue<ChunkBase>();

        [SerializeField]
        int _numChunksToInitializePerFrame = 4;

        [SerializeField]
        int _numChunksToSpawnPerAFrame = 4;

        [SerializeField]
        bool _spawnChunksProcedurally = true;

        JobHandle _initJobChunkDependencies;
        JobHandle _updateJobChunkDependencies;

        Vector3 _currentCenter;

        public bool IsUpdatingChunks { get; private set; }

        void Start()
        {
            _chunkPool = new ChunkPool(_chunkPrefab, this.transform);

            this.OnDestroyAsObservable()
                .Subscribe(_ =>
                {
                    _chunkPool.Dispose();
                });

            InitChunks();
            StartCoroutine("BuildInitialChunks");
        }

        void InitChunks()
        {
            for (var y = 0; y <= Mathf.Max(0, 2 * _ranges.y); y++)
            {
                for (var z = 0; z <= Mathf.Max(0, 2 * _ranges.z); z++)
                {
                    for (var x = 0; x <= Mathf.Max(0, 2 * _ranges.x); x++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _playerTransform.position + new Vector3(posX, posY, posZ) * ChunkSize;

                        var cPos = GetChunkPositionAt(chunkWorldPos);

                        var chunk = InitChunk(cPos);

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
            _chunks.Add(cPos, chunk);

            try
            {
                chunk.Initialize(this, ChunkSize, Extent, cPos);
            }
            catch (System.InvalidOperationException ex)
            {
                var outputLog = "failed to initialized chunk\n";
                outputLog += GetChunkAndNeighboursDebugInfo(chunk);
                outputLog += "\n" + ex.Message;
                Debug.LogAssertion(outputLog + ex.Message);
            }


            if (chunk is JobChunkBase jobChunk)
            {
                jobChunk.MarkUpdate();
            }

            return chunk;
        }

        void MarkUpdate(ChunkBase chunk)
        {
            // chunk.MarkUpdate();
            if (chunk is JobChunkBase jobChunk)
            {
                var scheduled = jobChunk.TryScheduleUpdateMeshJob();

                // if (!scheduled)
                // {
                //     Debug.LogWarning($"{chunk.Name} Not Scheduled");
                // }
            }
            else
            {
                chunk.MarkUpdate();
            }
        }

        IEnumerator BuildInitialChunks()
        {
            yield return null;

            _currentCenter = _playerTransform.position;

            // build chunk at from the player position to distant positions
            // do not update chunks at edge, because a chunk needs all 6 neighbours.
            for (var y = 0; y <= Mathf.Max(0, 2 * (_ranges.y - 1)); y++)
            {
                for (var z = 0; z <= Mathf.Max(0, 2 * (_ranges.z - 1)); z++)
                {
                    for (var x = 0; x <= Mathf.Max(0, 2 * (_ranges.x - 1)); x++)
                    {
                        // 0, -1, 1, -2, 2, ... -_ranges.x + 1, _ranges.x - 1
                        var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.z + 1, _ranges.z - 1
                        var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                        // 0, -1, 1, -2, 2, ... -_ranges.y + 1, _ranges.y - 1
                        var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                        var chunkWorldPos = _currentCenter + new Vector3(posX, posY, posZ) * ChunkSize;
                        var cPos = GetChunkPositionAt(chunkWorldPos);
                        if (_chunks.TryGetValue(cPos, out var chunk))
                        {
                            try
                            {
                                MarkUpdate(chunk);
                            }
                            catch (System.InvalidOperationException ex)
                            {
                                var outputLog = "failed to udpate chunk from BuildInitialChunks()\n";
                                outputLog += GetChunkAndNeighboursDebugInfo(chunk);
                                outputLog += "\n" + ex.Message;
                                Debug.LogAssertion(outputLog + ex.Message);
                            }

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

            yield return new WaitForSeconds(1f);

            IsWorldInitialized = true;
        }

        void Update()
        {
            if (!IsWorldInitialized || !_spawnChunksProcedurally || IsUpdatingChunks)
            {
                return;
            }

            IsUpdatingChunks = true;
            _currentCenter = _playerTransform.position;
            CheckActiveChunks();
            RemoveInactiveChunks();
            StartCoroutine("SpawnChunksInRange");
        }

        bool IsInRange(Vector3 chunkPos0, Vector3 chunkPos1, Vector3Int ranges)
        {
            var diffX = Mathf.Abs(chunkPos0.x - chunkPos1.x);
            var diffY = Mathf.Abs(chunkPos0.y - chunkPos1.y);
            var diffZ = Mathf.Abs(chunkPos0.z - chunkPos1.z);

            if (diffX > ranges.x * ChunkSize || diffY > ranges.y * ChunkSize || diffZ > ranges.z * ChunkSize)
            {
                return false;
            }

            return true;
        }

        void CheckActiveChunks()
        {
            if (_chunksToReturn.Count == 0)
            {
                var centerChunkPos = GetChunkPositionAt(_currentCenter);
                foreach (var chunk in _chunks.Values)
                {
                    if (!IsInRange(centerChunkPos, chunk.Position, _ranges))
                    {
                        _chunksToReturn.Enqueue(chunk);
                    }
                }

            }

            if (_chunkPositionsToSpawn.Count == 0)
            {
                for (var y = 0; y <= Mathf.Max(0, 2 * _ranges.y); y++)
                {
                    for (var z = 0; z <= Mathf.Max(0, 2 * _ranges.z); z++)
                    {
                        for (var x = 0; x <= Mathf.Max(0, 2 * _ranges.x); x++)
                        {
                            // 0, -1, 1, -2, 2, ... -_ranges.x, _ranges.x
                            var posX = x == 0 ? x : (x % 2 == 1 ? -(x / 2 + 1) : x / 2);

                            // 0, -1, 1, -2, 2, ... -_ranges.z, _ranges.z
                            var posZ = z == 0 ? z : (z % 2 == 1 ? -(z / 2 + 1) : z / 2);

                            // 0, -1, 1, -2, 2, ... -_ranges.y, _ranges.y
                            var posY = y == 0 ? y : (y % 2 == 1 ? -(y / 2 + 1) : y / 2);

                            var chunkWorldPos = _currentCenter + new Vector3(posX, posY, posZ) * ChunkSize;
                            var cPos = GetChunkPositionAt(chunkWorldPos);

                            if (!_chunks.TryGetValue(cPos, out var chunk) || chunk.NeedsUpdate)
                            {
                                _chunkPositionsToSpawn.Enqueue(cPos);
                            }
                        }
                    }
                }
            }

            // Debug.Log($"chunksToDestroy: {_chunksToDestroy.Count}, chunkPositionsToSpawn: {_chunkPositionsToSpawn.Count}");
        }

        void RemoveInactiveChunks()
        {
            while (_chunksToReturn.Count > 0)
            {
                var chunk = _chunksToReturn.Dequeue();

                _chunks.Remove(chunk.Position);
                _chunkPool.Return(chunk);
            }
        }

        IEnumerator SpawnChunksInRange()
        {
            var centerChunkPos = GetChunkPositionAt(_currentCenter);

            // first, initialize all chunks.
            // also, determinese which chunk to update.
            var initCount = 0;
            while (_chunkPositionsToSpawn.Count > 0)
            {
                var cPos = _chunkPositionsToSpawn.Dequeue();

                var chunk = InitChunk(cPos);

                // do not update chunks at edge, because a chunk needs all 6 neighbours.
                if (IsInRange(centerChunkPos, cPos, new Vector3Int(Mathf.Max(0, _ranges.x - 1), Mathf.Max(0, _ranges.y - 1), Mathf.Max(0, _ranges.z - 1))))
                {
                    _chunksToSpawn.Enqueue(chunk);
                }

                initCount++;
                if (_numChunksToInitializePerFrame <= initCount)
                {
                    initCount = 0;
                    yield return null;
                }
            }

            yield return null;

            // finally, start updating chunks.
            // this updates a certain number(_numChunksToSpawnInAFrame) of chunks in a frame.
            var spawnCount = 0;
            while (_chunksToSpawn.Count > 0)
            {
                var chunk = _chunksToSpawn.Dequeue();

                // Debug.Log($"Update Chunk={chunk.Name}");

                try
                {
                    MarkUpdate(chunk);
                }
                catch (System.InvalidOperationException ex)
                {
                    var outputLog = "failed to udpate chunk from SpawnChunksInRange()\n";
                    outputLog += GetChunkAndNeighboursDebugInfo(chunk);
                    outputLog += "\n" + ex.Message;
                    Debug.LogAssertion(outputLog + ex.Message);
                }


                spawnCount++;
                if (_numChunksToSpawnPerAFrame <= spawnCount)
                {
                    spawnCount = 0;
                    yield return null;
                }
            }

            yield return null;

            // Debug.Log($"{count} chunks spawned");
            IsUpdatingChunks = false;
        }

        public string GetChunkAndNeighboursDebugInfo(ChunkBase chunk)
        {
            var debugInfo = "";
            if (chunk is JobChunkBase jobChunk)
            {
                debugInfo = jobChunk.GetDebugInfo();
            }

            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                debugInfo += $"\n({side.ToString()}) ";
                if (TryGetNeighbourChunk(chunk, side, out var neighbourChunk))
                {
                    if (neighbourChunk is JobChunkBase neighbourJobChunk)
                    {
                        debugInfo += neighbourJobChunk.GetDebugInfo();
                    }
                    else
                    {
                        debugInfo += "Neighbour Chunk Found, but not JobChunkBase";
                    }
                }
                else
                {
                    debugInfo += "Neighbour Chunk Not Found\n";
                }
            }

            return debugInfo;
        }
    }
}
