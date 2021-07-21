using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using Losk.Fluid.FluidObjects;

namespace Losk.Trail
{
    /// <summary>
    /// パーティクル / エミッター
    /// </summary>
    public struct Particle
    {
        public int index;       // ID
        public Vector3 pos;     // 位置
        public Vector3 vel;     // 速度
        public Vector3 acc;     // 加速度
        public float dens;      // 密度
        public Vector3 force;   // 力
        public Vector3 prevPos; // 前ステップの位置
        public float scalingFactor;	// スケーリングファクタ
        public Vector3 deltaPos;	// 位置修正量
        public int hash;        // ハッシュ値

    }

    /// <summary>
    /// 空間分割法用のセル用構造体
    /// </summary>
    public struct CellStartEnd
    {
        public int startIdx;
        public int endIdx;
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

        public SwapBuffer _particleBuffer;          // パーティクル情報を詰めておく
        public ComputeBuffer _deltaDensBuffer;      // 密度変動量計算用バッファ
        public ComputeBuffer _cellStartEndBuffer;   // 空間分割法におけるセルの最初と最後を詰めておくバッファ
        public ComputeBuffer _particleIdBuffer;     // パーティクルのソートによってトレイルがぐちゃぐちゃになるのを防ぐバッファ

        [Header("シミュレーション設定")]
        public int _particleNum = 100;              // パーティクル数
        public float initRadius = 20f;              // パーティクルの初期設置半径
        public float _gatherPower = 0.1f;           // マウスインタラクション用
        public float _wallStiffness = 1.0f;         // 壁係数

        public Vector3 _spaceMin;                   // シミュレーション空間最小座標
        public Vector3 _spaceMax;                   // シミュレーション空間最大座標
        public Vector3 _gravity;                    // 重力

        /// <summary>
        /// シミュレーション用パラメータ
        /// </summary>

        public float _mass = 0.02f;                 // 質量
        public float _density = 998.29f;            // 初期密度
        public float _dt = 0.005f;                  // タイムステップ幅
        public float _viscosity = 0.001f;           // 粘性係数
        public float _kernelParticles = 20.0f;      // カーネル内のパーティクル数

        private float _effectiveRadius = 0.2f;      // 有効半径
        private float _volume;                      // カーネルパーティクル分の体積
        private float _particleRadius;              

        /// <summary>
        /// PBF用パラメータ
        /// </summary>
        [Header("PBF用パラメータ")]
        public float _epsilon = 0.001f;             // CFMの緩和係数
        public float _densFluctuation = 0.05f;      // 密度変動率
        public int _minIterations = 2;              // 最小ヤコビ反復回数
        public int _maxIterations = 10;             // 最大ヤコビ反復回数
        public bool _useArtificialPressure = true;  // 人工圧力の有無
        public float _ap_k = 0.1f;                  // 人工圧力係数k
        public float _ap_n = 4.0f;                  // 人工圧力係数n
        public float _ap_q = 0.2f;                  // 人工圧力係数q
        
        /// <summary>
        /// 近傍探索用グリッドの分割数
        /// </summary>
        public Vector3Int _nnSearchDivNum = new Vector3Int(10, 10, 10);


        [SerializeField, Header("References")]
        FluidSource _fluidSource;                   // 流体源, TODO: 複数対応
        
        [SerializeField]
        FluidDestination _fluidDestination;         // 流体出口, TODO: 複数対応



        /// <summary>
        /// groupの数
        /// </summary>
        /// <returns></returns>
        private Vector3Int _groupNum = new Vector3Int(1, 1, 1);

        private const int THREAD_NUM_X = 256;

        
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            // パーティクル数を2の累乗に丸める(BiotonicSort用)
            int r = Mathf.CeilToInt(Mathf.Log(_particleNum, 2));
            _particleNum = (int)Mathf.Pow(2, r);

            // 体積/有効半径/パーティクル半径計算
            _volume = _kernelParticles * _mass / _density; // カーネルパーティクル分の体積
            _effectiveRadius = Mathf.Pow((3.0f * _volume) / (4.0f * Mathf.PI), 1f / 3f); // 球の体積から有効半径を計算
            _particleRadius = Mathf.Pow((Mathf.PI / (6.0f * _kernelParticles)), 1f / 3f) * _effectiveRadius;

            // グループ数計算
            _groupNum.x = Mathf.CeilToInt(_particleNum / THREAD_NUM_X) + 1;

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
            _deltaDensBuffer = new ComputeBuffer(_particleNum, sizeof(float));
            _cellStartEndBuffer = new ComputeBuffer(_nnSearchDivNum.x * _nnSearchDivNum.y * _nnSearchDivNum.z, Marshal.SizeOf(typeof(CellStartEnd)));
            _particleIdBuffer = new ComputeBuffer(_particleNum, sizeof(int));

            _particleBuffer.Current.SetData(
                Enumerable.Range(0, _particleNum)
                .Select(_ => new Particle() { 
                    index = _,
                    pos = _fluidSource.GetRandomPositionInArea(),
                    vel = _fluidSource._initVelocity,
                    acc = Vector3.zero,
                    dens = 0.0f,
                    force = Vector3.zero,
                    prevPos = Vector3.zero,
                    scalingFactor = 0.0f,
                    deltaPos = Vector3.zero,
                }).ToArray()
            );

            _particleBuffer.Other.SetData(
                Enumerable.Range(0, _particleNum)
                .Select(_ => new Particle() {
                    index = _,
                    pos = Random.insideUnitSphere * initRadius, 
                    vel = Vector3.zero,
                    acc = Vector3.zero,
                    dens = 0.0f,
                    force = Vector3.zero,
                    prevPos = Vector3.zero,
                    scalingFactor = 0.0f,
                    deltaPos = Vector3.zero,
                }).ToArray()
            );

            _particleIdBuffer.SetData(
                Enumerable.Range(0, _particleNum).ToArray()
            );

            SetParameters();
        }

        /// <summary>
        /// GPUパラメータを設定
        /// </summary>
        private void SetParameters()
        {
            _particleComputeShader.SetInt(CS_NAMES.PARTICLE_NUM, _particleNum);
            _particleComputeShader.SetFloat(CS_NAMES.TIME, Time.time);
            _particleComputeShader.SetFloat(CS_NAMES.GATHER_POWER, _gatherPower);
            _particleComputeShader.SetFloat(CS_NAMES.DT, _dt);

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
            _particleComputeShader.SetVector(CS_NAMES.SOURCE_MIN, _fluidSource.GetMin());
            _particleComputeShader.SetVector(CS_NAMES.SOURCE_MAX, _fluidSource.GetMax());
            _particleComputeShader.SetVector(CS_NAMES.DESTINATION_MIN, _fluidDestination.GetMin());
            _particleComputeShader.SetVector(CS_NAMES.DESTINATION_MAX, _fluidDestination.GetMax());
            _particleComputeShader.SetVector(CS_NAMES.INIT_VELOCITY, _fluidSource._initVelocity);
            _particleComputeShader.SetVector(CS_NAMES.MOUSE_POS, new Vector4(-1000.0f, -1000.0f, -1000.0f, 0f));

            _particleComputeShader.SetInts(CS_NAMES.NNSEARCH_DIM, new int[]{_nnSearchDivNum.x, _nnSearchDivNum.y, _nnSearchDivNum.z});
        }

        /// <summary>
        /// シミュレーションを1step進める
        /// </summary>
        public void LocalUpdate()
        {
            SetParameters();

            ComputeSourceAndDestination();

            // ここからシミュレーションメインループ

            // ハッシュ値の計算
            ComputeHash();

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
            while ((densVar > _densFluctuation || iter < _minIterations) && (iter <= _maxIterations)) {
                // ハッシュ値の計算
                ComputeHash();

                ComputeScalingFactor();         // "caluculate λ_i"
                ComputePositionCorrection();    // "caluculate Δp_i"

                // 衝突処理のみ行う
                _particleComputeShader.SetBool(CS_NAMES.IS_COLLISION_ONLY_IN_INTEGRATE, true);
                Integrate();    // "perform collision detection and response"

                ApplyPositionCorrection();  // "update position x*_i <= x*_i + Δp_i"

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
            _deltaDensBuffer.Release();
            _cellStartEndBuffer.Release();
            _particleIdBuffer.Release();
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
        /// パーティクルの流入と流出の処理
        /// </summary>
        void ComputeSourceAndDestination()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_SOURCE_AND_DESTINATION);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 密度計算
        /// </summary>
        void ComputeDensity()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_DENSITY_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.CELL_START_END_BUFFER, _cellStartEndBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
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
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.CELL_START_END_BUFFER, _cellStartEndBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
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
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
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
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.CELL_START_END_BUFFER, _cellStartEndBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 位置修正量の計算
        /// </summary>
        void ComputePositionCorrection()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_POSITION_CORRECTION);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.CELL_START_END_BUFFER, _cellStartEndBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
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
            _particleComputeShader.Dispatch(kernelIdx_pc, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// 平均密度変動率を計算
        /// </summary>
        /// <returns></returns>
        float ComputeDensityFluctuation()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_DENSITY_FLUCTUATION);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.F_OUTPUT_BUFFER, _deltaDensBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();


            Losk.GPGPUCommon.InclusiveScan<float>(_deltaDensBuffer);

            float[] result = new float[1];
            _deltaDensBuffer.GetData(result, 0, _particleNum - 1, 1);
            return result[0] / (float)_particleNum;
        }

        /// <summary>
        /// 速度を更新
        /// </summary>
        void UpdateVelocity()
        {
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.UPDATE_VELOCITY);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();
        }

        /// <summary>
        /// ハッシュを計算し、ハッシュをもとにソートし、セルのStart/Endインデックスを格納
        /// hash == Maxは非表示パーティクル
        /// </summary>
        void ComputeHash()
        {
            // ハッシュを計算
            int kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_HASH_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
            _particleBuffer.Swap();

            
            // Biotonicソート (要素数が2の累乗である必要あり)
            kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.BIOTONIC_SORT_KERNEL);

            int nlog = (int)(Mathf.Log(_particleNum, 2));
            int inc, B_index;

            for (int i = 0; i < nlog; i++) {
                inc = 1 << i;
                for (int j = 0; j < i + 1; j++) {
                    B_index = 2;
                    _particleComputeShader.SetInt(CS_NAMES.INC, inc * 2 / B_index);
                    _particleComputeShader.SetInt(CS_NAMES.DIR, 2 << i);
                    _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
                    _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_WRITE_BUFFER, _particleBuffer.Other);
                    _particleComputeShader.Dispatch(kernelIdx, _particleNum / B_index /THREAD_NUM_X, _groupNum.y, _groupNum.z);
                    _particleBuffer.Swap();
                    inc /= B_index;
                }

            }

            // Start/EndのIDを検索して格納
            kernelIdx = _particleComputeShader.FindKernel(CS_NAMES.COMPUTE_CELL_START_END_KERNEL);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_READ_BUFFER, _particleBuffer.Current);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.CELL_START_END_BUFFER, _cellStartEndBuffer);
            _particleComputeShader.SetBuffer(kernelIdx, CS_NAMES.PARTICLE_ID_BUFFER, _particleIdBuffer);
            _particleComputeShader.Dispatch(kernelIdx, _groupNum.x, _groupNum.y, _groupNum.z);
        }

            void OnDrawGizmos()
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube((_spaceMin + _spaceMax) * 0.5f, (_spaceMax - _spaceMin));
#if UNITY_EDITOR
                UnityEditor.Handles.Label(_spaceMin, "Simulation Area");
#endif
            }
    }
}