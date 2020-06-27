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
        Vector3 _minCoordinates;

        public Vector3 MinCoordinates => _minCoordinates;

        [SerializeField]
        Vector3 _maxCoordinates;

        public Vector3 MaxCoordinates => _maxCoordinates;

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
