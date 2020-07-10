using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using UniRx;

namespace UniVoxel.Core
{
    public class SimpleWorld : WorldBase
    {
        [SerializeField]
        Vector3Int _minChunkPos = new Vector3Int(0, 0, 0);

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
                        cPos *= ChunkSize;
                        var chunk = Instantiate(_chunkPrefab, cPos, Quaternion.identity);
                        chunk.name = $"Chunk_{cPos.x}_{cPos.y}_{cPos.z}";
                        chunk.transform.SetParent(this.transform);

                        chunk.Initialize(this, ChunkSize, Extent, cPos);
                        _chunks.Add(cPos, chunk);
                    }
                }
            }
        }

        IEnumerator BuildChunks()
        {
            foreach (var chunk in _chunks.Values)
            {

                chunk.IsUpdatingChunkRP
                     .Pairwise()
                     .Where(isUpdating => isUpdating.Previous && !isUpdating.Current)
                     .FirstOrDefault()
                     .Subscribe(_ =>
                     {
                         OnComplete(chunk);
                     });

                chunk.MarkUpdate();
                yield return null;
            }

            yield return null;

            var playerPos = _player.transform.position;
            _player.transform.position = new Vector3(playerPos.z, playerPos.y + (_maxChunkPos.y + 1) * ChunkSize, playerPos.z);
            _player.gameObject.SetActive(true);

            LogChunkMeshData();
        }

        void LogChunkMeshData()
        {
            var _vertexCount = 0;

            foreach (var chunk in _chunks.Values)
            {
                var mesh = chunk.gameObject.GetComponent<MeshFilter>()?.mesh;
                if (mesh != null)
                {
                    _vertexCount += mesh.vertices.Length;

                }
                else
                {
                    Debug.LogWarning($"no mesh found in chunk={chunk.Name}");
                }
            }
            Debug.Log($"Current total vertex count: {_vertexCount}");

        }

        void OnComplete(ChunkBase chunk)
        {
            Debug.Log($"Complete updating chunk={chunk.Name}");
        }
    }
}
