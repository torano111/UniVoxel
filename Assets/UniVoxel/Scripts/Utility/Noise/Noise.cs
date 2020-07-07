﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using Unity.Collections;

namespace UniVoxel.Utility
{
    public static class JobPerlin
    {
        public static double GetOctavePerlin2D(double2 pos, int octaves, double persistence, NativeArray<int> permutation, int repeat = -1)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            double maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += GetPerlinNoise2D(pos * frequency, permutation, repeat) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public static double GetOctavePerlin3D(double3 pos, int octaves, double persistence, NativeArray<int> permutation, int repeat = -1)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            double maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += GetPerlinNoise3D(pos * frequency, permutation, repeat) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        static double GetPerlinNoise2D(double2 pos, NativeArray<int> permutation, int repeat = -1)
        {
            // If we have any repeat on, change the coordinates to their "local" repetitions
            if (repeat > 0)
            {
                pos.x = pos.x % repeat;
                pos.y = pos.y % repeat;
            }

            double floorX = math.floor(pos.x);
            double floorY = math.floor(pos.y);
            int xi = (int)floorX & 255;
            int yi = (int)floorY & 255;
            double xf = pos.x - (int)floorX;
            double yf = pos.y - (int)floorY;
            double u = Fade(xf);
            double v = Fade(yf);

            int aa, ab, ba, bb;
            aa = permutation[permutation[permutation[xi] + yi]];
            ab = permutation[permutation[permutation[xi] + Inc(yi, repeat)]];
            ba = permutation[permutation[permutation[Inc(xi, repeat)] + yi]];
            bb = permutation[permutation[permutation[Inc(xi, repeat)] + Inc(yi, repeat)]];

            double x1, x2, y1;
            x1 = Lerp(Grad2D(aa, xf, yf), Grad2D(ba, xf - 1, yf), u);
            x2 = Lerp(Grad2D(ab, xf, yf - 1), Grad2D(bb, xf - 1, yf - 1), u);
            y1 = Lerp(x1, x2, v);

            return (y1 + 1) / 2;
        }

        /// <summary>
        /// Calculates Perlin Noise at x, y, z.
        /// </summary>
        /// <returns> Returns value between 0 and 1 </returns>
        static double GetPerlinNoise3D(double3 pos, NativeArray<int> permutation, int repeat = -1)
        {
            // If we have any repeat on, change the coordinates to their "local" repetitions
            if (repeat > 0)
            {
                pos.x = pos.x % repeat;
                pos.y = pos.y % repeat;
                pos.z = pos.z % repeat;
            }

            // Calculate the "unit cube" that the point asked will be located in
            // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            // We also fade the location to smooth the result.

            // floor coordinates for negative values
            double floorX = math.floor(pos.x);
            double floorY = math.floor(pos.y);
            double floorZ = math.floor(pos.z);

            int xi = (int)floorX & 255;
            int yi = (int)floorY & 255;
            int zi = (int)floorZ & 255;
            double xf = pos.x - (int)floorX;
            double yf = pos.y - (int)floorY;
            double zf = pos.z - (int)floorZ;
            double u = Fade(xf);
            double v = Fade(yf);
            double w = Fade(zf);

            int aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = permutation[permutation[permutation[xi] + yi] + zi];
            aba = permutation[permutation[permutation[xi] + Inc(yi, repeat)] + zi];
            aab = permutation[permutation[permutation[xi] + yi] + Inc(zi, repeat)];
            abb = permutation[permutation[permutation[xi] + Inc(yi, repeat)] + Inc(zi, repeat)];
            baa = permutation[permutation[permutation[Inc(xi, repeat)] + yi] + zi];
            bba = permutation[permutation[permutation[Inc(xi, repeat)] + Inc(yi, repeat)] + zi];
            bab = permutation[permutation[permutation[Inc(xi, repeat)] + yi] + Inc(zi, repeat)];
            bbb = permutation[permutation[permutation[Inc(xi, repeat)] + Inc(yi, repeat)] + Inc(zi, repeat)];

            // The gradient function calculates the dot product between a pseudorandom
            // gradient vector and the vector from the input coordinate to the 8
            // surrounding points in its unit cube.
            // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
            // values we made earlier.
            double x1, x2, y1, y2;
            x1 = Lerp(Grad3D(aaa, xf, yf, zf), Grad3D(baa, xf - 1, yf, zf), u);
            x2 = Lerp(Grad3D(aba, xf, yf - 1, zf), Grad3D(bba, xf - 1, yf - 1, zf), u);
            y1 = Lerp(x1, x2, v);

            x1 = Lerp(Grad3D(aab, xf, yf, zf - 1), Grad3D(bab, xf - 1, yf, zf - 1), u);
            x2 = Lerp(Grad3D(abb, xf, yf - 1, zf - 1), Grad3D(bbb, xf - 1, yf - 1, zf - 1), u);
            y2 = Lerp(x1, x2, v);

            // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
            return (Lerp(y1, y2, w) + 1) / 2;
        }

        static int Inc(int num, int repeat)
        {
            num++;
            if (repeat > 0) num %= repeat;

            return num;
        }


        // get dot prodecut of (x, y) and one of 4(number of edges) gradient vectors
        // gradient vectors are:
        // (0, 1), (0, -1), (1, 0), (-1, 0)
        static double Grad2D(int hash, double x, double y)
        {
            switch (hash & 0x3)
            {
                case 0x0: return x + y;
                case 0x1: return -x + y;
                case 0x2: return x - y;
                case 0x3: return -x - y;
                default: return 0; // never happens
            }
        }

        // get dot prodecut of (x, y, z) and one of 16(number of edges) gradient vectors
        // gradient vectors are:
        // (1,1,0),(-1,1,0),(1,-1,0),(-1,-1,0),
        // (1,0,1),(-1,0,1),(1,0,-1),(-1,0,-1),
        // (0,1,1),(0,-1,1),(0,1,-1),(0,-1,-1)
        static double Grad3D(int hash, double x, double y, double z)
        {
            // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            int h = hash & 15;

            // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.
            double u = h < 8 /* 0b1000 */ ? x : y;

            // In Ken Perlin's original implementation this was another conditional operator (?:).  I
            // expanded it for readability.
            double v;

            // If the first and second significant bits are 0 set v = y
            if (h < 4 /* 0b0100 */)
                v = y;
            // If the first and second significant bits are 1 set v = x
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)
                v = x;
            // If the first and second significant bits are not equal (0/1, 1/0) set v = z
            else
                v = z;

            // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
            // dot(A, B) = A.x * B.x + A.y * B.y + A.z * B.z
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        // Fade function as defined by Ken Perlin.  This eases coordinate values
        // so that they will "ease" towards integral values.  This ends up smoothing
        // the final output.
        static double Fade(double t)
        {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        static double Lerp(double a, double b, double t)
        {
            // clamp t and then lerp
            return math.lerp(a, b, math.clamp(t, 0, 1));
        }
    }

    /// <summary>
    /// noise library in Unity.Mathematics. This can be used in a Job.
    /// </summary>
    public static class UnityNoise
    {
        public static float GetOctaveSimplex2D(float2 pos, int octaves, float persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            float maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += noise.snoise(pos * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public static float GetOctaveSimplex3D(float3 pos, int octaves, float persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            float maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += noise.snoise(pos * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }
    }

    /// <summary>
    /// Improved Perlin Noise implementation. This can NOT be used in a Job since it uses arrays and static variables.
    /// <see href="https://adrianb.io/2014/08/09/perlinnoise.html"> Understanding Perlin Noise </see>
    /// </summary>
    public static class Perlin
    {
        public static int repeat = -1;

        public static double GetOctavePerlin2D(double x, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            double maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += GetPerlinNoise2D(x * frequency, z * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public static double GetOctavePerlin3D(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;

            // Used for normalizing result to 0.0 - 1.0
            double maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += GetPerlinNoise3D(x * frequency, y * frequency, z * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        // Hash lookup table as defined by Ken Perlin.  This is a randomly
        // arranged array of all numbers from 0-255 inclusive.
        private static readonly int[] permutation = { 151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        // Doubled permutation to avoid overflow
        private static readonly int[] p;

        static Perlin()
        {
            p = new int[512];
            for (int x = 0; x < 512; x++)
            {
                p[x] = permutation[x % 256];
            }
        }

        static double GetPerlinNoise2D(double x, double y)
        {
            // If we have any repeat on, change the coordinates to their "local" repetitions
            if (repeat > 0)
            {
                x = x % repeat;
                y = y % repeat;
            }

            double floorX = Math.Floor(x);
            double floorY = Math.Floor(y);
            int xi = (int)floorX & 255;
            int yi = (int)floorY & 255;
            double xf = x - (int)floorX;
            double yf = y - (int)floorY;
            double u = Fade(xf);
            double v = Fade(yf);

            int aa, ab, ba, bb;
            aa = p[p[p[xi] + yi]];
            ab = p[p[p[xi] + Inc(yi)]];
            ba = p[p[p[Inc(xi)] + yi]];
            bb = p[p[p[Inc(xi)] + Inc(yi)]];

            double x1, x2, y1;
            x1 = Lerp(Grad2D(aa, xf, yf), Grad2D(ba, xf - 1, yf), u);
            x2 = Lerp(Grad2D(ab, xf, yf - 1), Grad2D(bb, xf - 1, yf - 1), u);
            y1 = Lerp(x1, x2, v);

            return (y1 + 1) / 2;
        }

        /// <summary>
        /// Calculates Perlin Noise at x, y, z.
        /// </summary>
        /// <returns> Returns value between 0 and 1 </returns>
        static double GetPerlinNoise3D(double x, double y, double z)
        {
            // If we have any repeat on, change the coordinates to their "local" repetitions
            if (repeat > 0)
            {
                x = x % repeat;
                y = y % repeat;
                z = z % repeat;
            }

            // Calculate the "unit cube" that the point asked will be located in
            // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            // We also fade the location to smooth the result.

            // floor coordinates for negative values
            double floorX = Math.Floor(x);
            double floorY = Math.Floor(y);
            double floorZ = Math.Floor(z);

            int xi = (int)floorX & 255;
            int yi = (int)floorY & 255;
            int zi = (int)floorZ & 255;
            double xf = x - (int)floorX;
            double yf = y - (int)floorY;
            double zf = z - (int)floorZ;
            double u = Fade(xf);
            double v = Fade(yf);
            double w = Fade(zf);

            int aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = p[p[p[xi] + yi] + zi];
            aba = p[p[p[xi] + Inc(yi)] + zi];
            aab = p[p[p[xi] + yi] + Inc(zi)];
            abb = p[p[p[xi] + Inc(yi)] + Inc(zi)];
            baa = p[p[p[Inc(xi)] + yi] + zi];
            bba = p[p[p[Inc(xi)] + Inc(yi)] + zi];
            bab = p[p[p[Inc(xi)] + yi] + Inc(zi)];
            bbb = p[p[p[Inc(xi)] + Inc(yi)] + Inc(zi)];

            // The gradient function calculates the dot product between a pseudorandom
            // gradient vector and the vector from the input coordinate to the 8
            // surrounding points in its unit cube.
            // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
            // values we made earlier.
            double x1, x2, y1, y2;
            x1 = Lerp(Grad3D(aaa, xf, yf, zf), Grad3D(baa, xf - 1, yf, zf), u);
            x2 = Lerp(Grad3D(aba, xf, yf - 1, zf), Grad3D(bba, xf - 1, yf - 1, zf), u);
            y1 = Lerp(x1, x2, v);

            x1 = Lerp(Grad3D(aab, xf, yf, zf - 1), Grad3D(bab, xf - 1, yf, zf - 1), u);
            x2 = Lerp(Grad3D(abb, xf, yf - 1, zf - 1), Grad3D(bbb, xf - 1, yf - 1, zf - 1), u);
            y2 = Lerp(x1, x2, v);

            // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
            return (Lerp(y1, y2, w) + 1) / 2;
        }

        static int Inc(int num)
        {
            num++;
            if (repeat > 0) num %= repeat;

            return num;
        }


        // get dot prodecut of (x, y) and one of 4(number of edges) gradient vectors
        // gradient vectors are:
        // (0, 1), (0, -1), (1, 0), (-1, 0)
        static double Grad2D(int hash, double x, double y)
        {
            switch (hash & 0x3)
            {
                case 0x0: return x + y;
                case 0x1: return -x + y;
                case 0x2: return x - y;
                case 0x3: return -x - y;
                default: return 0; // never happens
            }
        }

        // get dot prodecut of (x, y, z) and one of 16(number of edges) gradient vectors
        // gradient vectors are:
        // (1,1,0),(-1,1,0),(1,-1,0),(-1,-1,0),
        // (1,0,1),(-1,0,1),(1,0,-1),(-1,0,-1),
        // (0,1,1),(0,-1,1),(0,1,-1),(0,-1,-1)
        static double Grad3D(int hash, double x, double y, double z)
        {
            // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            int h = hash & 15;

            // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.
            double u = h < 8 /* 0b1000 */ ? x : y;

            // In Ken Perlin's original implementation this was another conditional operator (?:).  I
            // expanded it for readability.
            double v;

            // If the first and second significant bits are 0 set v = y
            if (h < 4 /* 0b0100 */)
                v = y;
            // If the first and second significant bits are 1 set v = x
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)
                v = x;
            // If the first and second significant bits are not equal (0/1, 1/0) set v = z
            else
                v = z;

            // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
            // dot(A, B) = A.x * B.x + A.y * B.y + A.z * B.z
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        // Fade function as defined by Ken Perlin.  This eases coordinate values
        // so that they will "ease" towards integral values.  This ends up smoothing
        // the final output.
        static double Fade(double t)
        {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        static double Lerp(double a, double b, double t)
        {
            // clamp t and then lerp
            // not work without clamp if coordinates are negative
            return a + Math.Min(Math.Max(0, t), 1) * (b - a);
        }
    }
}
