using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniVoxel.Core;
using UniVoxel.Utility;

namespace UniVoxel.Core
{
    [Serializable]
    public struct BlockData
    {
        [SerializeField]
        BlockType _blockType;
        public BlockType BlockType => _blockType;

        [SerializeField]
        Vector2Int _topFaceTexAtlasPos;

        [SerializeField]
        Vector2Int _bottomFaceTexAtlasPos;

        [SerializeField]
        Vector2Int _frontFaceTexexAtlasPos;

        [SerializeField]
        Vector2Int _backFaceTexexAtlasPos;

        [SerializeField]
        Vector2Int _leftFaceTexexAtlasPos;

        [SerializeField]
        Vector2Int _rightFaceTexexAtlasPos;

        public Vector2Int GetTexAtlasPosition(BoxFaceSide side)
        {
            switch (side)
            {
                case BoxFaceSide.Front:
                    return _frontFaceTexexAtlasPos;
                case BoxFaceSide.Back:
                    return _backFaceTexexAtlasPos;
                case BoxFaceSide.Right:
                    return _rightFaceTexexAtlasPos;
                case BoxFaceSide.Left:
                    return _leftFaceTexexAtlasPos;
                case BoxFaceSide.Top:
                    return _topFaceTexAtlasPos;
                case BoxFaceSide.Bottom:
                    return _bottomFaceTexAtlasPos;
                default:
                    throw new System.ArgumentException();
            }
        }
    }

    [CreateAssetMenu(fileName = "BlockData", menuName = "UniVoxel/BlockDataScriptableObject", order = 0)]
    public class BlockDataScriptableObject : ScriptableObject
    {
        [SerializeField]
        List<BlockData> _blockDataList = new List<BlockData>();

        Dictionary<BlockType, BlockData> _blockDataDictionary = new Dictionary<BlockType, BlockData>();

        public bool TryGetBlockData(BlockType blockType, out BlockData data)
        {
            if (_blockDataDictionary.TryGetValue(blockType, out data))
            {
                return true;
            }

            return false;
        }

        void OnEnable()
        {
            Initialize();
        }

        void Initialize()
        {
            foreach (var data in _blockDataList)
            {
                _blockDataDictionary.Add(data.BlockType, data);
            }
        }

        public Vector2 GetUVCoord00(BlockType blockType, BoxFaceSide side, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            if (TryGetBlockData(blockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                return BlockUtility.GetUV00FromTextureAtlas(texAtlasPos, singleTextureLengths, textureAtlasLengths);
            }

            throw new System.InvalidOperationException();
        }

        public Vector2 GetUVCoord11(BlockType blockType, BoxFaceSide side, Vector2 singleTextureLengths, Vector2 textureAtlasLengths)
        {
            if (TryGetBlockData(blockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                return BlockUtility.GetUV11FromTextureAtlas(texAtlasPos, singleTextureLengths, textureAtlasLengths);
            }

            throw new System.InvalidOperationException();
        }
    }
}
