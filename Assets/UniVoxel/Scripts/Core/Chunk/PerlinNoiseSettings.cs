using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UniVoxel.Core
{
    [Serializable]
    public struct PerlinNoise2DData
    {
        [SerializeField]
        int _maxGroundHeight;
        public int MaxGroundHeight => _maxGroundHeight;

        [SerializeField]
        int _maxStoneLayerHeight;

        public int MaxStoneLayerHeight => _maxStoneLayerHeight;

        [SerializeField]
        int _minHeight;

        public int MinHeight => _minHeight;

        [SerializeField]
        float _heightNoiseScaler;

        public float HeightNoiseScaler => _heightNoiseScaler;

        [SerializeField]
        int _heightNoiseOctaves;

        public int HeightNoiseOctaves => _heightNoiseOctaves;

        [SerializeField]
        double _heightNoisePersistence;

        public double HeightNoisePersistence => _heightNoisePersistence;
    }
    
    [Serializable]
    public struct PerlinNoise3DData
    {

        [SerializeField]
        double _densityThreshold;

        public double DensityThreshold => _densityThreshold;

        [SerializeField]
        float _densityNoiseScaler;

        public float DensityNoiseScaler => _densityNoiseScaler;

        [SerializeField]
        int _densityNoiseOctaves;

        public int DensityNoiseOctaves => _densityNoiseOctaves;

        [SerializeField]
        double _densityNoisePersistence;

        public double DensityNoisePersistence => _densityNoisePersistence;
    }

    [CreateAssetMenu(fileName = "PerlinNoiseSettings", menuName = "UniVoxel/PerlinNoiseSettings", order = 0)]
    public class PerlinNoiseSettings : ScriptableObject
    {
        [SerializeField]
        PerlinNoise2DData _2dData;
        public PerlinNoise2DData Noise2D => _2dData;

        [SerializeField]
        bool _useNoise2D = true;

        public bool UseNoise2D => _useNoise2D;

        [SerializeField]
        PerlinNoise3DData _3dData;
        public PerlinNoise3DData Noise3D => _3dData;

        [SerializeField]
        bool _useNoise3D = true;

        public bool UseNoise3D => _useNoise3D;
    }
}
