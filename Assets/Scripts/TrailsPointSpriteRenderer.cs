using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk.Trail
{
    /// <summary>
    /// GPUTrailのRenderer
    /// </summary>
    public class TrailsPointSpriteRenderer : MonoBehaviour
    {
        [SerializeField]
        GPUTrailsController _trailController;

        [SerializeField]
        PBFParticleController _particleController;

        public Material _material;

        void OnRenderObject()
        {
            _material.SetBuffer(CS_NAMES.PARTICLE_BUFFER, _particleController._particleBuffer.Current);
            _material.SetPass(0);

            int particleNum = _particleController.ParticleNum;
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleNum);

        }
    }
}