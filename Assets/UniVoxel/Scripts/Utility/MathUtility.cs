using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace UniVoxel.Utility
{
    public static class MathUtility
    {
        /// <summary>
        /// get 1D array index from 3D array indices
        /// </summary>
        public static int GetLinearIndexFrom3Points(int x, int y, int z, int xLength, int zLength)
        {
            return (xLength * zLength * y) + (xLength * z) + x;
        }

        public static int3 Get3DIndicesFromLinearIndex(int index, int xLength, int zLength)
        {
            var xzl = xLength * zLength;
            var planeIndex = index % xzl;
            var x = planeIndex % xLength;
            var z = planeIndex / xLength;
            var y = index / xzl;
            return new int3(x, y, z);
        }
    }
}
