using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk.Trail
{
    /// <summary>
    /// GPUTrailのRenderer
    /// </summary>
    public class TrailsRenderer : MonoBehaviour
    {
        [SerializeField]
        GPUTrailsController _trailController;

        [SerializeField]
        PBFParticleController _particleController;

        public Material _material;

        void OnRenderObject()
        {
            _material.SetInt(CS_NAMES.NODE_NUM_PER_TRAIL, _trailController._nodeNumPerTrail);
            _material.SetFloat(CS_NAMES.LIFE, _trailController._life);
            _material.SetFloat(CS_NAMES.TIME, Time.time);
            _material.SetBuffer(CS_NAMES.TRAIL_BUFFER, _trailController._trailBuffer);
            _material.SetBuffer(CS_NAMES.NODE_BUFFER, _trailController._nodeBuffer);
            _material.SetPass(0);

            int nodeNumPerTrail = _trailController._nodeNumPerTrail;
            int particleNum = _particleController._particleNum;
            Graphics.DrawProceduralNow(MeshTopology.Points, nodeNumPerTrail, particleNum);

        }
    }
}