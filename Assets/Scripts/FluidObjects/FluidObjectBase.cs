using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk.Fluid.FluidObjects
{
    /// <summary>
    /// 流体オブジェトベース
    /// </summary>
    public class FluidObjectBase : MonoBehaviour
    {
        public enum FluidObjectType
        {
            Cuboid = 0,     // 直方体
            // Sphere = 1      // 球
        }

        /// <summary>
        /// 流体源タイプ
        /// </summary>
        public FluidObjectType _fluidSourceType;


        /// <summary>
        /// 半径
        /// </summary>
        // public float _radius = 1f;

        /// <summary>
        /// 中心位置を取得
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public Vector3 GetMin()
        {
            return (transform.position - transform.localScale * 0.5f);
        }
        public Vector3 GetMax()
        {
            return (transform.position + transform.localScale * 0.5f);
        }

        /// <summary>
        /// エリア内のランダムな点を返す
        /// </summary>
        public Vector3 GetRandomPositionInArea()
        {
            return new Vector3(Random.Range(GetMin().x, GetMax().x), Random.Range(GetMin().y, GetMax().y), Random.Range(GetMin().z, GetMax().z));
        }

        /// <summary>
        /// スケールを取得
        /// </summary>
        public Vector3 GetSize()
        {
            return transform.localScale;
        }

        

        protected virtual void OnDrawGizmos()
        {
            if (_fluidSourceType == FluidObjectType.Cuboid) {
                Gizmos.DrawWireCube(GetPosition(), GetSize());
#if UNITY_EDITOR
                UnityEditor.Handles.Label(GetPosition(), gameObject.name);
#endif
            }
        }
    }

}