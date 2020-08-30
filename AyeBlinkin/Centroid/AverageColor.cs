//#undef DEBUG
using System.Drawing;

namespace AyeBlinkin.Centroid 
{
    internal class AverageColor : ICentroidColor
    {
        private const int startCluster = 1;
        private const int grayThreshold = 10;

        public byte[] Calculate(ref byte[] memBuffer, ref Rectangle[] recs, int recindex, int scan) 
        {
            if(recindex >= recs.Length)
                return new byte[0];

            var rec = recs[recindex];
            int r = 0, g = 0, b = 0, x = 0, padding = 4 * (scan - rec.Width),
                width = rec.Width, end = width * rec.Height, 
                ix = (((width * 4) + padding) * rec.Y) + (rec.X * 4);

            while(x < end) {
                r += memBuffer[ix+2];
                g += memBuffer[ix+1];
                b += memBuffer[ix]; 

                ix += 4;
                if(++x % width == 0)
                    ix += padding;
            }

            var result = new byte[] { (byte)(r/end), (byte)(g/end), (byte)(b/end) };

#if DEBUG
            if(recindex == Settings.Model.PreviewLED) 
            {
                CentroidBase target = new CentroidBase() { memberCount = 1 };
                
                target.mean[0] = result[0];
                target.mean[1] = result[1];
                target.mean[2] = result[2];
                
                CentroidColorForm.setBackground(ref memBuffer, ref rec, padding);
                CentroidColorForm.setColors(new CentroidBase[] { target });
            }
#endif
            //rgb must be byte in range 0-254
            if(result[0] == 0xFF) result[0] = 0xFE;
            if(result[1] == 0xFF) result[1] = 0xFE;
            if(result[2] == 0xFF) result[2] = 0xFE;

            return result;
        }
    }
}