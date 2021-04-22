using UnityEngine;

namespace Losk
{
    /// <summary>
    /// スワップバッファ
    /// </summary>
    public class SwapBuffer
    {
        private　ComputeBuffer[] _buffers = new ComputeBuffer[2];

        public ComputeBuffer Current => _buffers[0];
        public ComputeBuffer Other => _buffers[1];

        private int _count = 0;
        private int _stride = 0;

        public int Count => _count;
        public int Stride => _stride;

        public SwapBuffer(int count, int stride)
        {
            _count = count;
            _stride = stride;

            for (int i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = new ComputeBuffer(_count, _stride);
            }
        }

        public void Swap()
        {
            ComputeBuffer temp = _buffers[0];
            _buffers[0] = _buffers[1];
            _buffers[1] = temp;
        }

        public void Release()
        {
            foreach (var buf in _buffers)
            {
                buf.Release();
            }
        }
    }
}