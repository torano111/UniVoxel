using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

namespace UniVoxel.Utility
{
    [RequireComponent(typeof(SingleBlockViewer))]
    public class SingleBlockViewerUpdater : MonoBehaviour
    {
        [SerializeField]
        Button _createBlockButton;

        SingleBlockViewer _viewer;

        void Awake()
        {
            _viewer = GetComponent<SingleBlockViewer>();
        }

        void Start()
        {
            _createBlockButton.OnClickAsObservable()
                              .Subscribe(_ => 
                              {
                                  _viewer.CreateBlock();
                              });
        }
    }
}
