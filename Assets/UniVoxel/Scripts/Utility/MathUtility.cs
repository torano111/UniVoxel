﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}
