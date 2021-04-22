using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Losk
{
    /// <summary>
    /// ComputeShaderを使う上でのツール
    /// </summary>
    public static class GPGPUCommon
    {
        private const int THREAD_SIZE_X = 8;

        private static ComputeShader _cs;
        private static bool _isInitialized = false;

        private static class KernelIndexes
        {
            public static int iInclusiveScanId;
            public static int fInclusiveScanId;
            public static int iExclusiveScanId;
            public static int fExclusiveScanId;
            public static int iCopyResultToInputId;
            public static int fCopyResultToInputId;
        }

        private static class VariableIndexes
        {
            public static int elementNumId;
            public static int loopNumId;
            public static int iInputId;
            public static int iOutputId;
            public static int fInputId;
            public static int fOutputId;
        }

        private static void InitializeCheck()
        {
            if (!_isInitialized) {
                _cs = (ComputeShader)Resources.Load("GPGPUCommon");

                // Kernels
                KernelIndexes.iInclusiveScanId = _cs.FindKernel("iInclusiveScan");
                KernelIndexes.fInclusiveScanId = _cs.FindKernel("fInclusiveScan");
                KernelIndexes.iExclusiveScanId = _cs.FindKernel("iExclusiveScan");
                KernelIndexes.fExclusiveScanId = _cs.FindKernel("fExclusiveScan");
                KernelIndexes.iCopyResultToInputId = _cs.FindKernel("iCopyResultToInput");
                KernelIndexes.fCopyResultToInputId = _cs.FindKernel("fCopyResultToInput");

                // Variables
                VariableIndexes.elementNumId = Shader.PropertyToID("_elementNum");
                VariableIndexes.loopNumId = Shader.PropertyToID("_loopNum");
                VariableIndexes.iInputId = Shader.PropertyToID("_iInput");
                VariableIndexes.iOutputId = Shader.PropertyToID("_iOutput");
                VariableIndexes.fInputId = Shader.PropertyToID("_fInput");
                VariableIndexes.fOutputId = Shader.PropertyToID("_fOutput");
            }
        }

        /// <summary>
        /// Inclusive Scan (Prefix Sum, Shorter span, more parallel)
        /// </summary>
        /// <param name="cb">Compute Buffer (int or float)</param>
        /// <typeparam name="T">Compute Buffer element type (only int or float)</typeparam>
        public static void InclusiveScan<T>(ComputeBuffer cb)
        {
            InitializeCheck();

            int n = cb.count;
            int s = cb.stride;

            int scanId, copyId, inputId, outputId;

            if (typeof(T) == typeof(int) && s == sizeof(int)) {
                scanId = KernelIndexes.iExclusiveScanId;
                copyId = KernelIndexes.iCopyResultToInputId;
                inputId = VariableIndexes.iInputId;
                outputId = VariableIndexes.iOutputId;
            }
            else if (typeof(T) == typeof(float) && s == sizeof(float)) {
                scanId = KernelIndexes.fInclusiveScanId;
                copyId = KernelIndexes.fCopyResultToInputId;
                inputId = VariableIndexes.fInputId;
                outputId = VariableIndexes.fOutputId;
            }
            else {
                throw new System.FormatException("Only compute buffers of int and float are targeted by this function.");
            }


            ComputeBuffer result = new ComputeBuffer(n, cb.stride);
            _cs.SetInt(VariableIndexes.elementNumId, n);

            int groupNum = Mathf.CeilToInt((float)n / (float)THREAD_SIZE_X);
            for (int i = 0; i < Mathf.CeilToInt(Mathf.Log((float)n, 2)) - 1; i++) {
                _cs.SetBuffer(scanId, inputId, cb);
                _cs.SetBuffer(scanId, outputId, result);
                _cs.SetInt(VariableIndexes.loopNumId, i);
                _cs.Dispatch(scanId, groupNum, 1, 1);

                _cs.SetBuffer(copyId, inputId, cb);
                _cs.SetBuffer(copyId, outputId, result);
                _cs.Dispatch(copyId, groupNum, 1, 1);
            }

            result.Release();
        }

        /// <summary>
        /// Exlusive Scan (Prefix Sum, Shorter span, more parallel)
        /// </summary>
        /// <param name="cb">Compute Buffer (int or float)</param>
        /// <typeparam name="T">Compute Buffer element type (only int or float)</typeparam>
        public static void ExclusiveScan<T>(ComputeBuffer cb)
        {
            InitializeCheck();

            int n = cb.count;
            int s = cb.stride;

            int scanId, copyId, inputId, outputId;

            if (typeof(T) == typeof(int) && s == sizeof(int)) {
                scanId = KernelIndexes.iExclusiveScanId;
                copyId = KernelIndexes.iCopyResultToInputId;
                inputId = VariableIndexes.iInputId;
                outputId = VariableIndexes.iOutputId;
            }
            else if (typeof(T) == typeof(float) && s == sizeof(float)) {
                scanId = KernelIndexes.fExclusiveScanId;
                copyId = KernelIndexes.fCopyResultToInputId;
                inputId = VariableIndexes.fInputId;
                outputId = VariableIndexes.fOutputId;
            }
            else {
                throw new System.FormatException("Only compute buffers of int and float are targeted by this function.");
            }


            ComputeBuffer result = new ComputeBuffer(n + 1, cb.stride);
            _cs.SetInt(VariableIndexes.elementNumId, n);

            int groupNum = Mathf.CeilToInt((float)n / (float)THREAD_SIZE_X);
            for (int i = 0; i < Mathf.CeilToInt(Mathf.Log((float)n, 2)) - 1; i++) {
                _cs.SetBuffer(scanId, inputId, cb);
                _cs.SetBuffer(scanId, outputId, result);
                _cs.SetInt(VariableIndexes.loopNumId, i);
                _cs.Dispatch(scanId, groupNum, 1, 1);

                _cs.SetBuffer(copyId, inputId, cb);
                _cs.SetBuffer(copyId, outputId, result);
                _cs.Dispatch(copyId, groupNum, 1, 1);
            }

            result.Release();
        }
    }

}