using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    public static class BlockUtility
    {
        public static Vector2 GetUV00FromTextureAtlas(Vector2Int positionOnTextureAtlas, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            return new Vector2(singleTextureLengths.x / textureAtlasLengths.x * positionOnTextureAtlas.x, singleTextureLengths.y / textureAtlasLengths.y * positionOnTextureAtlas.y) + new Vector2(0.001f, 0.001f);
        }

        public static Vector2 GetUV11FromTextureAtlas(Vector2Int positionOnTextureAtlas, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            return new Vector2(singleTextureLengths.x / textureAtlasLengths.x * (positionOnTextureAtlas.x + 1), singleTextureLengths.y / textureAtlasLengths.y * (positionOnTextureAtlas.y + 1)) - new Vector2(0.001f, 0.001f);
        }

        public static Vector3Int GetNeighbourPosition(int x, int y, int z, BoxFaceSide neighbourDirection, int offset = 1)
        {
            switch (neighbourDirection)
            {
                case BoxFaceSide.Front:
                    z += offset;
                    break;
                case BoxFaceSide.Back:
                    z -= offset;
                    break;
                case BoxFaceSide.Right:
                    x += offset;
                    break;
                case BoxFaceSide.Left:
                    x -= offset;
                    break;
                case BoxFaceSide.Top:
                    y += offset;
                    break;
                case BoxFaceSide.Bottom:
                    y -= offset;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            return new Vector3Int(x, y, z);
        }
    }
}
