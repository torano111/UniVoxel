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

    public class Block
    {
        public Block(BlockType blockType)
        {
            this.BlockType = blockType;
        }
        public BlockType BlockType { get; set; }
    }
}
