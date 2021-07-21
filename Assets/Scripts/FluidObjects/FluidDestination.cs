using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk.Fluid.FluidObjects
{
    /// <summary>
    /// 流体の流出位置
    /// </summary>
    public class FluidDestination : FluidObjectBase
    {
        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            base.OnDrawGizmos();
        }
        
    }

}