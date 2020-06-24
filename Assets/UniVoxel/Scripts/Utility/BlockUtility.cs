using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    public static class BlockUtility
    {
        readonly static Vector2 _littleSpace = new Vector2(0.001f, 0.001f);
        public static Vector2 GetUV00FromTextureAtlas(Vector2Int positionOnTextureAtlas, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            return new Vector2(singleTextureLengths.x / textureAtlasLengths.x * positionOnTextureAtlas.x, singleTextureLengths.y / textureAtlasLengths.y * positionOnTextureAtlas.y) + _littleSpace;
        }

        public static Vector2 GetUV11FromTextureAtlas(Vector2Int positionOnTextureAtlas, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            return new Vector2(singleTextureLengths.x / textureAtlasLengths.x * (positionOnTextureAtlas.x + 1), singleTextureLengths.y / textureAtlasLengths.y * (positionOnTextureAtlas.y + 1)) - _littleSpace;
        }

        public static Vector3Int GetNeighbourPosition(int x, int y, int z, BoxFaceSide neighbourDirection)
        {
            switch (neighbourDirection)
            {
                case BoxFaceSide.Front:
                    z++;
                    break;
                case BoxFaceSide.Back:
                    z--;
                    break;
                case BoxFaceSide.Right:
                    x++;
                    break;
                case BoxFaceSide.Left:
                    x--;
                    break;
                case BoxFaceSide.Top:
                    y++;
                    break;
                case BoxFaceSide.Bottom:
                    y--;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            return new Vector3Int(x, y, z);
        }
    }
}
