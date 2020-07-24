using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Core;
using UniRx;
using UniRx.Triggers;

namespace UniVoxel.GamePlay
{
    [RequireComponent(typeof(PlayerCore))]
    public class PlayerBlockEditor : MonoBehaviour
    {
        WorldBase World => WorldBase.Instance;

        public enum BlockEditMode
        {
            None,
            Add,
            Remove,
        }

        [SerializeField]
        BlockEditMode _editMode = BlockEditMode.Add;

        /// <summary>
        /// try if we add block on mouse button. false if remove.
        /// </summary>
        public BlockEditMode EditMode { get => _editMode; set => _editMode = value; }

        [SerializeField]
        BlockType _blockType = BlockType.Grass;

        public BlockType BlockType { get => _blockType; set => _blockType = value; }

        [SerializeField]
        LayerMask _chunkMask;

        public LayerMask ChunkMask { get => _chunkMask; set => _chunkMask = value; }

        [SerializeField]
        float _maxEditDistance = 10f;

        public float MaxEditDistance { get => _maxEditDistance; set => _maxEditDistance = value; }

        PlayerCore _playerCore;
        BlockEditor _blockEditor = new BlockEditor();

        public bool CanEdit { get; set; }

        Queue<ChunkBase> _chunksToUpdate = new Queue<ChunkBase>();


        void Awake()
        {
            _playerCore = GetComponent<PlayerCore>();

            CanEdit = true;
        }

        void Start()
        {
            this.UpdateAsObservable()
                .Where(_ => _playerCore.IsInitialized)
                .Where(_ => CanEdit)
                .Where(_ => Input.GetMouseButtonDown(0))
                .Subscribe(_ =>
                {
                    EditBlock();
                });

            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.LeftShift))
                .Subscribe(_ =>
                {
                    if (EditMode == BlockEditMode.Add)
                    {
                        EditMode = BlockEditMode.Remove;
                    }
                    else if (EditMode == BlockEditMode.Remove)
                    {
                        EditMode = BlockEditMode.Add;
                    }
                });
        }

        void MarkChunksUpdate()
        {
            var updateInfo = "PlayerBlockEditor: Update";
            while (_chunksToUpdate.Count > 0)
            {
                var chunk = _chunksToUpdate.Dequeue();
                // chunk.MarkModified();

                updateInfo += $" {chunk.Name}";
            }

            Debug.Log(updateInfo);
        }

        void EditBlock()
        {
            _blockEditor.ChunkLayerMask = ChunkMask;

            switch (EditMode)
            {
                case BlockEditMode.None:
                    break;
                case BlockEditMode.Add:
                    AddBlock();
                    break;
                case BlockEditMode.Remove:
                    RemoveBlock();
                    break;
            }
        }

        bool IsBlockOnEdge(ChunkBase chunk, Vector3Int indices, out Vector3Int neighbourOffset)
        {
            neighbourOffset = chunk.Position;

            if (chunk.IsBlockOnEdge(indices.x, indices.y, indices.z))
            {
                for (var axis = 0; axis < 3; axis++)
                {
                    // if the difference is negative(should be -1), then the actual position in the neighbour chunk is neighbour size - 1.
                    if (indices[axis] == 0)
                    {
                        neighbourOffset[axis] -= chunk.Size;
                        continue;
                    }

                    // if the difference is positive(should be +1), then the actual position in the neighbour chunk is 0.
                    else if (indices[axis] == chunk.Size - 1)
                    {
                        neighbourOffset[axis] += chunk.Size;
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        void CheckNeighboursToUpdate(BlockEditInfo editInfo)
        {
            if (IsBlockOnEdge(editInfo.Chunk, editInfo.BlockIndices, out var neighbourOffset))
            {
                Vector3Int neighbourChunkPos;
                for (var axis = 0; axis < 3; axis++)
                {
                    neighbourChunkPos = editInfo.Chunk.Position;
                    neighbourChunkPos[axis] = neighbourOffset[axis];

                    if (neighbourChunkPos == editInfo.Chunk.Position)
                    {
                        continue;
                    }

                    if (World.TryGetChunkAt(neighbourChunkPos, out var neighbourChunk))
                    {
                        _chunksToUpdate.Enqueue(neighbourChunk);
                    }
                    else
                    {
                        Debug.LogAssertion("The edited block is on the edge of the chunk, but no neighbour chunk found");
                    }
                }
            }
        }

        void AddBlock()
        {
            if (_blockEditor.RaycastAndAddBlock(_playerCore.PlayerCamera.transform.position, _playerCore.PlayerCamera.transform.forward, MaxEditDistance, BlockType, out var editInfo))
            {
                _chunksToUpdate.Enqueue(editInfo.Chunk);

                CheckNeighboursToUpdate(editInfo);
                MarkChunksUpdate();
            }
        }

        void RemoveBlock()
        {
            if (_blockEditor.RaycastAndRemoveBlock(_playerCore.PlayerCamera.transform.position, _playerCore.PlayerCamera.transform.forward, MaxEditDistance, out var editInfo))
            {
                _chunksToUpdate.Enqueue(editInfo.Chunk);

                CheckNeighboursToUpdate(editInfo);
                MarkChunksUpdate();
            }
        }
    }
}
