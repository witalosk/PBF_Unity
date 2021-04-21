using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Losk.Trail
{
    /// <summary>
    /// 各Update関数の実行順を一元管理するためのもの
    /// 実行順はInspecterで指定する
    /// </summary>
    public class WholeUpdater : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> _beUpdatedObjects;
        void Update()
        {
            for (int i = 0; i < _beUpdatedObjects.Count; i++) {
                _beUpdatedObjects[i].GetComponent<BeUpdatedInterface>().LocalUpdate();
            }
        }
    }
}