using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Losk.Trail;

namespace Losk.Fluid.FluidObjects
{
    /// <summary>
    /// 流体内の衝突物オブジェクト
    /// </summary>
    public class FluidCollider : FluidObjectBase
    {
        [HideInInspector]
        public List<Vector3> _pointList;    // パーティクルの頂点
        private Vector3 _initPosition;      // 初期位置

        [SerializeField]
        private PBFParticleController _particleController;

        private float _ptpDistance;         // 点と点の間の距離

        private void Start()
        {
            _initPosition = transform.position;
            CaluculatePointList();
        }

        /// <summary>
        /// 点群の位置を計算
        /// </summary>
        private void CaluculatePointList()
        {
            _ptpDistance = Mathf.Pow(_particleController._mass / _particleController._density, 1f / 3f);
            int count = 0;

            for (int x = 0; x < (int)(transform.localScale.x / _ptpDistance) + 1; x++) {
                for (int y = 0; y < (int)(transform.localScale.y / _ptpDistance) + 1; y++) {
                    for (int z = 0; z < (int)(transform.localScale.z / _ptpDistance) + 1; z++) {
                        _pointList.Add(new Vector3(_ptpDistance * x, _ptpDistance * y, _ptpDistance * z));
                        count++;
                    }
                }
            }

            Debug.Log("distance: " + _ptpDistance + ", count: " + count);
            
        }

        /// <summary>
        /// 初期位置からの移動量を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMoveAmount()
        {
            return transform.position - _initPosition;
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            base.OnDrawGizmos();
        }
        
    }

}