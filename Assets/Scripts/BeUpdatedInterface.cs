using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Losk.Trail
{
    interface BeUpdatedInterface
    {
        /// <summary>
        /// それぞれのオブジェクト個別のUpdate関数
        /// </summary>
        void LocalUpdate();
    }
}