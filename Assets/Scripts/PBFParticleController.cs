using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;

namespace Losk.Trail
{
    /// <summary>
    /// パーティクル / エミッター
    /// </summary>
    public struct Particle
    {
        public Vector3 pos;     // 位置
        public Vector3 vel;     // 速度
        public Vector3 acc;     // 加速度
        public float dens;      // 密度
        public Vector3 force;   // 力
        public Vector3 prevPos; // 前ステップの位置
        public Vector3 prevVel; // 前ステップの速度
        public float scalingFactor;	// スケーリングファクタ
        public Vector3 deltaPos;	// 位置修正量
        public float deltaDens; // 密度変動量

    }

    /// <summary>
    /// パーティクル(Emitter)を動かすコントローラ
    /// </summary>
    public class PBFParticleController : MonoBehaviour, BeUpdatedInterface
    {
        /// <summary>
        /// パーティクル用コンピュートシェーダ
        /// </summary>
        [SerializeField]
        ComputeShader _particleComputeShader;

        public SwapBuffer _particleBuffer;

        /// <summary>
        /// パーティクルの数
        /// </summary>
        public int _particleNum = 100;
        public int ParticleNum { get{ return _particleNum; } set { value = _particleNum; } }
    
        public float _timeScale = 1.0f;
        public float _positionScale = 1.0f;
        public float _noiseScale = 1.0f;
        public float initRadius = 20f;
        public float _gatherPower = 0.1f;
        public float _wallStiffness = 1.0f;

        public Vector3 _spaceMin;
        public Vector3 _spaceMax;
        public Vector3 _gravity;

        /// <summary>
        /// シミュレーション用パラメータ
        /// </summary>
        public float _effectiveRadius = 0.2f;      // 有効半径
        public float _mass = 0.02f;                 // 質量

        public float _density = 998.29f;
        public float _dt = 0.005f;
        public float _viscosity = 0.001f;
        public float _kernelParticles = 20.0f;

        private float _volume;
        private float _particleRadius;

        /// <summary>
        /// PBF用パラメータ
        /// </summary>
        public float _epsilon = 0.001f;      // CFMの緩和係数
        public float _densFluctuation = 0.05f;  // 密度変動率
        public int _minIterations = 2;          // 最小ヤコビ反復回数
        public int _maxIterations = 10;         // 最大ヤコビ反復回数
        public bool _useArtificialPressure = true;  // 人工圧力
        public float _ap_k = 0.1f;                  // 人工圧力係数k
        public float _ap_n = 4.0f;                  // 人工圧力係数n
        public float _ap_q = 0.2f;                  // 人工圧力係数q
        
        /// <summary>
        /// 初期化処理
        /// </summary>
        void Start()
        {
            // 体積/有効半径/パーティクル半径計算
            _volume = _kernelParticles * _mass / _density; // カーネルパーティクル分の体積
            _effectiveRadius = Mathf.Pow((3.0f * _volume) / (4.0f * Mathf.PI), 1f / 3f); // 球の体積から半径を計算
            _particleRadius = Mathf.Pow((Mathf.PI / (6.0f * _kernelParticles)), 1f / 3f) * _effectiveRadius;
            Debug.Log("ParticleRadius: " + _particleRadius);


            // カーネル関数の係数部分定義
            _particleComputeShader.SetFloat(CS_NAMES.WPOLY6, 315f / (64f * Mathf.PI * Mathf.Pow(_effectiveRadius, 9f)));
            _particleComputeShader.SetFloat(CS_NAMES.G_WPOLY6, -945f / (32f * Mathf.PI * Mathf.Pow(_effectiveRadius, 9f)));
            _particleComputeShader.SetFloat(CS_NAMES.L_WPOLY6, -945f / (32f * Mathf.PI * Mathf.Pow(_effectiveRadius, 9f)));
            _particleComputeShader.SetFloat(CS_NAMES.WSPIKY, 15f/(Mathf.PI * Mathf.Pow(_effectiveRadius, 6f)));
            _particleComputeShader.SetFloat(CS_NAMES.G_WSPIKY, -45f/(Mathf.PI * Mathf.Pow(_effectiveRadius, 6f)));
            _particleComputeShader.SetFloat(CS_NAMES.L_WSPIKY, -90f/(Mathf.PI * Mathf.Pow(_effectiveRadius, 6f)));
            _particleComputeShader.SetFloat(CS_NAMES.WVISC, 15f / (2f * Mathf.PI * Mathf.Pow(_effectiveRadius, 3f)));
            _particleComputeShader.SetFloat(CS_NAMES.G_WVISC, 15f / (2f * Mathf.PI * Mathf.Pow(_effectiveRadius, 3f)));
            _particleComputeShader.SetFloat(CS_NAMES.L_WVISC, 45f / (Mathf.PI * Mathf.Pow(_effectiveRadius, 6f)));

            // バッファの初期化
            _particleBuffer = new SwapBuffer(_particleNum, Marshal.SizeOf(typeof(Particle)));


            _particleBuffer.Current.SetData(
                Enumerable.Range(0, _particleNum)
                .Select(_ => new Particle() { 
                    pos = Random.insideUnitSphere * initRadius, 
                    vel = Vector3.zero,
                    acc = Vector3.zero,
                    dens = 0.0f,
                    force = Vector3.zero,
                    prevPos = Vector3.zero,
                    prevVel = Vector3.zero,
                    scalingFactor = 0.0f,
                    deltaPos = Vector3.zero,
                    deltaDens = 0.0f
                }).ToArray()
            );

            _particleBuffer.Other.SetData(
                Enumerable.Range(0, _particleNum)
                .Select(_ => new Particle() { 
                    pos = Random.insideUnitSphere * initRadius, 
                    vel = Vector3.zero,
                    acc = Vector3.zero,
                    dens = 0.0f,
                    force = Vector3.zero,
                    prevPos = Vector3.zero,
                    prevVel = Vector3.zero,
                    scalingFactor = 0.0f,
                    deltaPos = Vector3.zero,
                    deltaDens = 0.0f
                }).ToArray()
            );
            _particleComputeShader.SetVector(CS_NAMES.GRAVITY, _gravity);
            _particleComputeShader.SetFloat(CS_NAMES.EFFECTIVE_RADIUS, _effectiveRadius);
            _particleComputeShader.SetFloat(CS_NAMES.MASS, _mass);
            _particleComputeShader.SetFloat(CS_NAMES.VISCOSITY, _viscosity);
            _particleComputeShader.SetFloat(CS_NAMES.DENSITY, _density);
            _particleComputeShader.SetFloat(CS_NAMES.EPSILON, _epsilon);
            _particleComputeShader.SetBool(CS_NAMES.USE_ARTIFICIAL_PRESSURE, _useArtificialPressure);
            _particleComputeShader.SetFloat(CS_NAMES.AP_K, _ap_k);
            _particleComputeShader.SetFloat(CS_NAMES.AP_N, _ap_n);
            _particleComputeShader.SetFloat(CS_NAMES.AP_Q, _ap_q);
            _particleComputeShader.SetFloat(CS_NAMES.AP_WQ, KernelPoly6(_ap_q * _effectiveRadius, _effectiveRadius, 315f / (64f * Mathf.PI * Mathf.Pow(_effectiveRadius, 9f))));
            _particleComputeShader.SetFloat(CS_NAMES.WALL_STIFFNESS, _wallStiffness);

            _particleComputeShader.SetVector(CS_NAMES.SPACE_MIN, _spaceMin);
            _particleComputeShader.SetVector(CS_NAMES.SPACE_MAX, _spaceMax);
            _particleComputeShader.SetVector(CS_NAMES.MOUSE_POS, new Vector4(-1000.0f, -1000.0f, -1000.0f, 0f));
        }

        public void LocalUpdate()
        {
            _particleComputeShader.SetInt(CS_NAMES.PARTICLE_NUM, _particleNum);
            _particleComputeShader.SetFloat(CS_NAMES.TIME, Time.time);
            _particleComputeShader.SetFloat(CS_NAMES.TIME_SCALE, _timeScale);
            _particleComputeShader.SetFloat(CS_NAMES.POSITION_SCALE, _positionScale);
            _particleComputeShader.SetFloat(CS_NAMES.NOISE_SCALE, _noiseScale);
            _particleComputeShader.SetFloat(CS_NAMES.GATHER_POWER, _gatherPower);
            _particleComputeShader.SetFloat(CS_NAMES.DT, _dt);

            // ここからメインループ

            // SPHによる密度計算 
            ComputeDensity();

            // 外力項による力場の計算
            ComputeExternalForces();

            // 予測位置・速度の更新
            _particleComputeShader.SetBool(CS_NAMES.IS_COLLISION_ONLY_IN_INTEGRATE, false);
            Integrate();

            // 位置修正反復
            int iter = 0;           // 反復回数
            float densVar = 1.0f;   // 密度の分散
            while ((densVar > _densFluctuation) || (iter < _minIterations) && (iter < _maxIterations)) {
                ComputeScalingFactor();         // caluculate λ_i
                ComputePositionCorrection();    // caluculate Δp_i

                // 衝突処理のみ行う
                _particleComputeShader.SetBool(CS_NAMES.IS_COLLISION_ONLY_IN_INTEGRATE, true);
                Integrate();    // perform collision detection and response

                ApplyPositionCorrection();  // update position x*_i <= x*_i + Δp_i

                densVar = ComputeDensityFluctuation();

                if (densVar <= _densFluctuation && iter > _minIterations) break;

                iter++;
            }
            
            // 速度の更新
            UpdateVelocity();
        }

        void OnDestroy()
        {
            _particleBuffer.Release();
        }

        /// <summary>
        /// Poly6カーネルの関数値の計算
        /// </summary>
        /// <param name="r"></param>
        /// <param name="h"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        float KernelPoly6(float r, float h, float a)
        {
            if(r >= 0.0 && r <= h){
                float q = h*h-r*r;
                return a*q*q*q;
            }
            else{
                return 0.0f;
            }
        }

        /// <summary>
        /// 密度計算
        /// </summary>
        void ComputeDensity()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_DENSITY_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }
    
        /// <summary>
        /// パーティクルにかかる力の計算(外力項)
        /// </summary>
        void ComputeExternalForces()
        {
            // マウスとのインタラクション
            if (Input.GetMouseButton(0)) {
                Vector3 screenPos = Input.mousePosition;
                screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
                _particleComputeShader.SetVector(CS_NAMES.MOUSE_POS, Camera.main.ScreenToWorldPoint(screenPos));
            }
            else {
                _particleComputeShader.SetVector(CS_NAMES.MOUSE_POS, new Vector4(-1000.0f, -1000.0f, -1000.0f, 0f));
            }

            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_EXTERNAL_FORCES_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 予測位置・速度の計算
        /// </summary>
        void Integrate()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.INTEGRATE_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// スケーリングファクタの計算
        /// </summary>
        void ComputeScalingFactor()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_SCALING_FACTOR);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 位置修正量の計算と位置修正
        /// </summary>
        void ComputePositionCorrection()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_POSITION_CORRECTION);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 位置修正を適用
        /// </summary>
        void ApplyPositionCorrection()
        {
            int kernelIdx_pc = _particleComputeShader.FindKernel(CS_NAMES.POSITION_CORRECTION);
            _particleComputeShader.SetBuffer(kernelIdx_pc, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx_pc, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx_pc, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 平均密度変動率を計算 (exclusive scanがないので現状こうなっている)
        /// </summary>
        /// <returns></returns>
        float ComputeDensityFluctuation()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_POSITION_CORRECTION);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();

            float sum = 0.0f;
            Particle[] temp = new Particle[_particleNum];
            _particleBuffer.Current.GetData(temp);
            for (int i = 0; i < _particleNum; i++) {
                sum += temp[i].deltaDens;
            }

            return sum / (float)_particleNum;
        }

        /// <summary>
        /// 速度を更新
        /// </summary>
        void UpdateVelocity()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.UPDATE_VELOCITY);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _particleNum, 1, 1);
            _particleBuffer.Swap();
        }
    }

}