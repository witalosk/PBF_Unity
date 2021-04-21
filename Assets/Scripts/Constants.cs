using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Losk.Trail
{
    /// <summary>
    /// ComputeShader内の変数名
    /// </summary>
    public class CS_NAMES
    {
        // 変数名
        public const string PARTICLE_NUM = "_particleNum";
        public const string TIME = "_time";
        public const string TIME_SCALE = "_timeScale";
        public const string POSITION_SCALE = "_positionScale";
        public const string NOISE_SCALE = "_noiseScale";
        public const string UPDATE_DISTANCE_MIN = "_updateDistaceMin";
        public const string NODE_NUM_PER_TRAIL = "_nodeNumPerTrail";
        public const string LIFE = "_life";
        public const string GATHER_POWER = "_gatherPower";
        public const string MOUSE_POS = "_mousePos";
        public const string SPACE_MIN = "_spaceMin";
        public const string SPACE_MAX = "_spaceMax";
        public const string GRAVITY = "_gravity";
        public const string EFFECTIVE_RADIUS = "_effectiveRadius";
        public const string MASS = "_mass";
        public const string VISCOSITY = "_viscosity";
        public const string DT = "_dt";
        public const string DENSITY = "_density";
        public const string EPSILON = "_epsilon";
        public const string USE_ARTIFICIAL_PRESSURE = "_useArtificialPressure";
        public const string AP_K = "_ap_k";
        public const string AP_N = "_ap_n";
        public const string AP_Q = "_ap_q";
        public const string AP_WQ = "_ap_wq";
        public const string IS_COLLISION_ONLY_IN_INTEGRATE = "_isCollisionOnlyInIntegrate";
        public const string WALL_STIFFNESS = "_wallStiffness";

        // カーネル関数(window function)
        public const string WPOLY6 = "_wpoly6";
        public const string G_WPOLY6 = "_gWpoly6";
        public const string L_WPOLY6 = "_lWpoly6";
        public const string WSPIKY = "_wspiky";
        public const string G_WSPIKY = "_gWspiky";
        public const string L_WSPIKY = "_lWspiky";
        public const string WVISC = "_wvisc";
        public const string G_WVISC = "_gWvisc";
        public const string L_WVISC = "_lWvisc";

        
        // バッファ
        public const string PARTICLE_READ_BUFFER = "_particleReadBuffer";
        public const string PARTICLE_WRITE_BUFFER = "_particleWriteBuffer";
        public const string PARTICLE_BUFFER = "_particleBuffer";
        public const string TRAIL_BUFFER = "_trailBuffer";
        public const string NODE_BUFFER = "_nodeBuffer";

        // カーネル名
        public const string UPDATE_KERNEL = "Update";
        public const string COMPUTE_DENSITY_KERNEL = "ComputeDensity";
        public const string COMPUTE_EXTERNAL_FORCES_KERNEL = "ComputeExternalForces";
        public const string INTEGRATE_KERNEL = "Integrate";
        public const string COMPUTE_SCALING_FACTOR = "ComputeScalingFactor";
        public const string COMPUTE_POSITION_CORRECTION = "ComputePositionCorrection";
        public const string POSITION_CORRECTION = "PositionCorrection";
        public const string COMPUTE_DENSITY_FLUCTUATION = "ComputeDensityFluctuation";
        public const string UPDATE_VELOCITY = "UpdateVelocity";
        public const string WRITE_TO_INPUT_KERNEL = "WriteToInput";
        public const string CALC_INPUT_KERNEL = "CalcInput";
    }

}