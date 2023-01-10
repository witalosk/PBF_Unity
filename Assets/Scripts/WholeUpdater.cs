using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Losk.Trail
{
    /// <summary>
    /// 各Update関数の実行順を一元管理するためのもの
    /// 実行順はInspectorで指定する
    /// </summary>
    public class WholeUpdater : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> _beUpdatedObjects;
        
        private readonly List<ILocalUpdate> _localUpdates = new List<ILocalUpdate>();

        private void Start()
        {
            foreach (var obj in _beUpdatedObjects)
            {
                _localUpdates.Add(obj.GetComponent<ILocalUpdate>());
            }
        }

        private void Update()
        {
            foreach (var localUpdate in _localUpdates)
            {
                localUpdate.LocalUpdate();
            }
        }
    }
}