using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotater : MonoBehaviour
{
    public float _speed = 1.0f;

    private float _deg = 0.0f;

    void Update()
    {
        transform.localRotation = Quaternion.Euler(0.0f, _deg, 0.0f);
        _deg += Time.deltaTime * _speed;
    }
}
