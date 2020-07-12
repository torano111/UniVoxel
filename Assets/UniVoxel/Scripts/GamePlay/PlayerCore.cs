using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace UniVoxel.GamePlay
{
    public class PlayerCore : MonoBehaviour
    {
        ReactiveProperty<bool> _isInitializedRP = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsInitializedRP => _isInitializedRP;

        public bool IsInitialized { get => IsInitializedRP.Value; protected set => _isInitializedRP.Value = value; }

        protected virtual void InitPlayer()
        {
            // initialization
            
            IsInitialized = true;
        }
    }
}
