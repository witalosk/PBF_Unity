using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk.Fluid.FluidObjects
{
    /// <summary>
    /// 流体の流入源
    /// </summary>
    public class FluidSource : FluidObjectBase
    {
        /// <summary>
        /// 初期速度
        /// </summary>
        public Vector3 _initVelocity;

        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            base.OnDrawGizmos();

            Gizmos.DrawLine(GetPosition(), GetPosition() + _initVelocity);
        }
    }

}