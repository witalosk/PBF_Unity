using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Losk.Trail
{
    interface ILocalUpdate
    {
        /// <summary>
        /// それぞれのオブジェクト個別のUpdate関数
        /// </summary>
        void LocalUpdate();
    }
}