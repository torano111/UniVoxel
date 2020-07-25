using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniVoxel.Core
{
    public struct BlockEditInfo
    {
        public ChunkBase Chunk { get; set; }
        public Vector3Int BlockIndices { get; set; }
    }

    public class BlockModifier
    {
        public LayerMask ChunkLayerMask { get; set; }
        protected WorldBase World => WorldBase.Instance;


        public bool TryGetBlock(Vector3 worldPos, out Block block)
        {
            if (World.TryGetChunkAt(worldPos, out var chunk))
            {
                var blockPos = chunk.GetBlockIndicesAt(worldPos);
                return chunk.TryGetBlock(blockPos.x, blockPos.y, blockPos.z, out block);
            }
            else
            {
                Debug.LogWarning("BlockModifier: No chunk found");
            }

            block = default(Block);
            return false;
        }

        public bool TrySetBlock(Vector3 worldPos, Block block, out BlockEditInfo editInfo)
        {
            editInfo = new BlockEditInfo();

            if (World.TryGetChunkAt(worldPos, out var chunk))
            {
                var blockPos = chunk.GetBlockIndicesAt(worldPos);
                chunk.SetBlock(blockPos.x, blockPos.y, blockPos.z, block);

                editInfo.Chunk = chunk;
                editInfo.BlockIndices = blockPos;
                return true;
            }
            else
            {
                Debug.LogWarning("BlockModifier: No chunk found");
            }

            return false;
        }


        public bool TryAddBlock(Vector3 worldPos, BlockType blockType, out BlockEditInfo editInfo)
        {
            var b = new Block(blockType);

            if (TryGetBlock(worldPos, out var block))
            {
                if (block.IsSolid)
                {
                    editInfo = new BlockEditInfo();
                    Debug.LogWarning("Try adding a block but already existed.");
                    return false;
                }
                else
                {
                    return TrySetBlock(worldPos, b, out editInfo);
                }
            }

            editInfo = new BlockEditInfo();
            return false;
        }

        public bool TryRemoveBlock(Vector3 worldPos, out BlockEditInfo editInfo)
        {
            var airBlock = default(Block);
            airBlock.IsSolid = false;

            if (TryGetBlock(worldPos, out var block))
            {

                if (!block.IsSolid)
                {
                    editInfo = new BlockEditInfo();
                    Debug.LogWarning("Try removing a block but already air.");
                    return false;

                }
                else
                {
                    return TrySetBlock(worldPos, airBlock, out editInfo);
                }
            }

            editInfo = new BlockEditInfo();
            return false;
        }

        public bool RaycastAndAddBlock(Vector3 origin, Vector3 direction, float maxDistance, BlockType blockType, out BlockEditInfo editInfo)
        {
            if (Physics.Raycast(origin, direction, out var hitInfo, maxDistance, ChunkLayerMask))
            {
                var blockWorldPos = hitInfo.point + hitInfo.normal * 0.5f;
                return TryAddBlock(blockWorldPos, blockType, out editInfo);
            }
            else
            {
                Debug.LogWarning("BlockModifier: Not hit");
            }

            editInfo = new BlockEditInfo();
            return false;
        }

        public bool RaycastAndRemoveBlock(Vector3 origin, Vector3 direction, float maxDistance, out BlockEditInfo editInfo)
        {
            if (Physics.Raycast(origin, direction, out var hitInfo, maxDistance, ChunkLayerMask))
            {
                var blockWorldPos = hitInfo.point + hitInfo.normal * -1f * 0.5f;
                return TryRemoveBlock(blockWorldPos, out editInfo);
            }
            else
            {
                Debug.LogWarning("BlockModifier: Hit but not chunk");
            }

            editInfo = new BlockEditInfo();
            return false;
        }
    }
}
