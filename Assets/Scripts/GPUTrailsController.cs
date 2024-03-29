using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine.Assertions;

namespace Losk.Trail
{
    /// <summary>
    /// GPUTrail本体
    /// </summary>
    public class GPUTrailsController : MonoBehaviour, ILocalUpdate
    {
        /// <summary>
        /// Trail
        /// </summary>
        public struct Trail
        {
            public int currentNodeIdx; // 最後に書き込んだNode Buffer Idx
        }

        /// <summary>
        /// Trailのノード
        /// </summary>
        public struct Node
        {
            public float time; //更新時間
            public Vector3 pos;
            public float alpha;
        }


        [SerializeField]
        PBFParticleController _particleController;

        [SerializeField]
        ComputeShader _trailsComputeShader;

        /// <summary>
        /// バッファ (ノードはループバッファ)
        /// </summary>
        public ComputeBuffer _trailBuffer, _nodeBuffer;

        /// <summary>
        /// トレイル１つ当たりのノード数
        /// </summary>
        public int _nodeNumPerTrail = 25;

        public float _updateDistanceMin = 0.01f;

        public float _life = 10.0f;


        void Start()
        {
            Assert.IsNotNull(_trailsComputeShader);

            const float MAX_FPS = 60f;
            _nodeNumPerTrail = Mathf.CeilToInt(_life * MAX_FPS);

            int particleNum = _particleController._particleNum;

            // バッファの初期化
            _trailBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(Trail)));
            _nodeBuffer = new ComputeBuffer(particleNum * _nodeNumPerTrail, Marshal.SizeOf(typeof(Node)));

            Trail initTrail = new Trail() { currentNodeIdx = -1 };
            Node initNode = new Node() { time = -1 };

            _trailBuffer.SetData(Enumerable.Repeat<Trail>(initTrail, particleNum).ToArray());
            _nodeBuffer.SetData(Enumerable.Repeat<Node>(initNode, particleNum * _nodeNumPerTrail).ToArray());
        }

        public void LocalUpdate()
        {
            int particleNum = _particleController._particleNum;

            int kernelIdx = _trailsComputeShader.FindKernel(CS_NAMES.CALC_INPUT_KERNEL);
            _trailsComputeShader.SetFloat(CS_NAMES.TIME, Time.time);
            _trailsComputeShader.SetFloat(CS_NAMES.UPDATE_DISTANCE_MIN, _updateDistanceMin);
            _trailsComputeShader.SetInt(CS_NAMES.PARTICLE_NUM, particleNum);
            _trailsComputeShader.SetInt(CS_NAMES.NODE_NUM_PER_TRAIL, _nodeNumPerTrail);
            _trailsComputeShader.SetInt(CS_NAMES.INVISIBLE_HASH, _particleController.NnSearchDivNum.x * _particleController.NnSearchDivNum.y * _particleController.NnSearchDivNum.z);
            _trailsComputeShader.SetBuffer(kernelIdx, CS_NAMES.TRAIL_BUFFER, _trailBuffer);
            _trailsComputeShader.SetBuffer(kernelIdx, CS_NAMES.NODE_BUFFER, _nodeBuffer);
            _trailsComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_BUFFER, _particleController._particleBuffer.Current);
            _trailsComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_ID_BUFFER, _particleController._particleIdBuffer);
            
            _trailsComputeShader.Dispatch(kernelIdx, Mathf.CeilToInt((float)particleNum / 256f), 1, 1);
        }

        void OnDestroy()
        {
            _trailBuffer.Release();
            _nodeBuffer.Release();
        }
    }

}