﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Core;
using UniRx;

namespace UniVoxel.Core.Debugging
{
    public class WorldHighestSolidBlockChecker : MonoBehaviour
    {
        [SerializeField]
        WorldBase _world;

        [SerializeField]
        bool _checkBoxCast = true;

        [SerializeField]
        bool _outputLog = true;

        [SerializeField]
        Vector3 _boxExtents = new Vector3(0.49f, 2f, 0.49f);

        [SerializeField]
        LayerMask _chunkMask;

        public bool IsCheckingHighestSolidBlock { get; protected set; }

        void Awake()
        {
            _world.IsWorldInitializedRP
                  .Subscribe(initialized => IsCheckingHighestSolidBlock = initialized);
        }

        void OnDrawGizmos()
        {
            if (IsCheckingHighestSolidBlock)
            {
                if (_checkBoxCast)
                {
                    var worldPos = transform.position;
                    worldPos.y = _world.WorldSettingsData.MaxCoordinates.y;
                    var maxDistance = Mathf.Abs(_world.WorldSettingsData.MaxCoordinates.y - _world.WorldSettingsData.MinCoordinates.y);
                    var hit = Physics.BoxCast(worldPos, _boxExtents, Vector3.down, out var hitInfo, Quaternion.identity, maxDistance, _chunkMask);

                    if (hit)
                    {
                        Gizmos.DrawRay(worldPos, Vector3.down * hitInfo.distance);
                        Gizmos.DrawWireCube(worldPos + Vector3.down * hitInfo.distance, _boxExtents * 2f);
                        
                        if (_outputLog)
                        {
                            Debug.Log($"WorldHighestSolidBlockChecker: Box position={worldPos + Vector3.down * hitInfo.distance}");
                        }
                    }
                    else
                    {
                        Gizmos.DrawRay(worldPos, Vector3.down * maxDistance);
                    }

                }
            }
        }
    }
}
