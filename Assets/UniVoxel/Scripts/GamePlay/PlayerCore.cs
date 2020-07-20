using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using UniVoxel.Core;

namespace UniVoxel.GamePlay
{
    public class PlayerCore : MonoBehaviour
    {
        protected WorldBase World => WorldBase.Instance;
        
        [SerializeField]
        Vector3 _sizes = new Vector3(0.8f, 1.8f, 0.8f);

        public Vector3 Sizes => _sizes;

        [SerializeField]
        Camera _playerCamera;

        public Camera PlayerCamera 
        {
            get 
            {
                if (_playerCamera == null)
                {
                    _playerCamera = Camera.main;
                }

                return _playerCamera;
            }
        }

        ReactiveProperty<bool> _isInitializedRP = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsInitializedRP => _isInitializedRP;

        public bool IsInitialized { get => IsInitializedRP.Value; protected set => _isInitializedRP.Value = value; }

        public CharacterController CharacterController;

        protected virtual void Awake()
        {
            CharacterController = GetComponent<CharacterController>();

            if (World == null)
            {
                Debug.Log("Player could not find the world");
                InitPlayer();
            }
            else
            {
                World.IsWorldInitializedRP
                     .Where(initialized => initialized)
                     .FirstOrDefault()
                     .Subscribe(_ =>
                     {
                         InitPlayer();
                     });
            }
        }

        // initialization
        protected virtual void InitPlayer()
        {
            SpawnPlayer();
            IsInitialized = true;
        }

        protected virtual void SpawnPlayer()
        {
            if (World == null)
            {
                return;
            }

            var pos = transform.position;
            if (World.BoxCastAndGetHighestSolidBlockIndices(pos, Sizes / 2f, out var chunk, out var blockIndices))
            {
                var spawnPos = new Vector3(pos.x, chunk.Position.y + blockIndices.y * chunk.Extent * 2, pos.z);
                Debug.Log($"Spawn Player: {spawnPos}, Chunk: {chunk.Name}, BlockIndices: {blockIndices.ToString()}");

                if (CharacterController)
                {
                    CharacterController.Move(spawnPos - pos);
                }
                else
                {
                    transform.SetPositionAndRotation(spawnPos, transform.rotation);
                }
                transform.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogAssertion("failed to spawn player");
            }
        }
    }
}
