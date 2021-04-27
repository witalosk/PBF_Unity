using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Losk
{
    /// <summary>
    /// FPSを表示
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] 
        Text _textObject;

        [SerializeField]
        uint _skipFrameNum = 1;

        float _fpsSum = 0f;
        int _addNum = 0;

        void Update()
        {
            if (Time.frameCount % (_skipFrameNum + 1) == 0) {
                _fpsSum += 1.0f / Time.deltaTime;
                _addNum++;
                _textObject.text = (1.0f / Time.deltaTime).ToString("f1") + "fps (avg: " + (_fpsSum / (float)_addNum).ToString("f1") + "fps)";
            }
        }
    }
}