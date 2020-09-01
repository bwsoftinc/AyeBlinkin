using System;
using System.Drawing;
using System.Collections.Generic;

using AyeBlinkin.Forms;

namespace AyeBlinkin.Centroid 
{
    internal abstract class CentroidBase 
    {
        //has an impact on CPU usage and latency by filtering out more shadres of gray
        //the absolute difference across r, g, b to be considered a shade of gray
        private const int grayThreshold = 20; 
        public int padding;
        public int x;
        public int y;
        public int width;
        public int height;
        public int end;
        public int bufferIndex;
        public byte[] buffer;
        public int pointIndex;
        public int stride;
        private Rectangle rec;

        public Rectangle rectangle { get => rec; set {
            width = value.Width;
            height = value.Height;
            x = value.X;
            y = value.Y;
            end = width * height;
            padding = 4 * (stride - width);
            bufferIndex = (((width * 4) + padding) * y) + (x * 4);
            rec = value;
        }}

        protected bool isGray(byte r, byte g, byte b) {
            return Math.Abs(r-g) <= grayThreshold 
                && Math.Abs(g-b) <= grayThreshold 
                && Math.Abs(r-b) <= grayThreshold;
        }

        public CentroidColor[] Clusters;
        public readonly byte[] mean = new byte[3];
        public readonly List<byte[]> members = new List<byte[]>();
        public void Calculate() 
        {
            Implementation();
           
#if DEBUG
            if(pointIndex == Settings.PreviewLED) 
                CentroidColorForm.Update(this);
#endif
            //rgb must be byte in range 0-254
            if(mean[0] == 0xFF) mean[0] = 0xFE;
            if(mean[1] == 0xFF) mean[1] = 0xFE;
            if(mean[2] == 0xFF) mean[2] = 0xFE;
        }
        protected abstract void Implementation();
    }
}