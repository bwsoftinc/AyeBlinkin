using System;
using System.Diagnostics;

namespace AyeBlinkin.DirectX
{
    internal class AvgFPSCounter : IDisposable
    {
        private const int framesToAverage = 30;
        private const float msScale = 1000F * framesToAverage;
        private float msTotal = 0F;
        private int dataPointer = 0;
        private float[] data = new float[framesToAverage];
        private Stopwatch timer = new Stopwatch();
        public AvgFPSCounter() => timer.Start();

        public void Dispose() {
            timer?.Stop();
            timer = null;
        }

        public double NextFPS() 
        {
            var ms = (float)timer.Elapsed.TotalMilliseconds;
            timer.Restart();

            msTotal += ms - data[dataPointer];
            data[dataPointer] = ms;

            dataPointer = ++dataPointer % framesToAverage;
            return msScale / msTotal;
        }
    }
}