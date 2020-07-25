using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniVoxel.Core
{
    public enum BoxFaceSide
    {
        Front,
        Back,
        Top,
        Bottom,
        Right,
        Left,
    };

    public enum BlockType
    {
        Grass,
        Dirt,
        Stone,
    }

    public struct Block
    {
        public Block(BlockType blockType)
        {
            this.BlockType = blockType;
            IsSolid = true;
        }

        public BlockType BlockType { get; set; }

        // false by default
        public bool IsSolid { get; set; }
    }
}
