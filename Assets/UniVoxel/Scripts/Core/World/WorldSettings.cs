using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UniVoxel.Core
{
    [Serializable]
    public struct WorldData
    {
        [SerializeField]
        float _coordinateLimit;

        public Vector3 GetMinCoordinate()
        {
            return new Vector3(-_coordinateLimit, -_coordinateLimit, -_coordinateLimit);
        }

        public Vector3 GetMaxCoordinate()
        {
            return new Vector3(_coordinateLimit, _coordinateLimit, _coordinateLimit);
        }

        [SerializeField]
        int _chunkSize;

        public int ChunkSize => _chunkSize;

        [SerializeField]
        float _extent;

        public float Extent => _extent;
    }

    [CreateAssetMenu(fileName = "WorldSettings", menuName = "UniVoxel/WorldSettings", order = 0)]
    public class WorldSettings : ScriptableObject
    {
        [SerializeField]
        WorldData _data;

        public WorldData Data => _data;
    }
}
