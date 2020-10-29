namespace AyeBlinkin.Centroid 
{
    internal class AverageColor : CentroidBase
    {
        private byte[] centroidMean;
        private CentroidColor gray, color;

        public AverageColor() 
        {
            Clusters = new CentroidColor[2] {
                gray = new CentroidColor(),
                color = new CentroidColor()
            };
        }

        protected override void Implementation()
        {
            byte r, g, b;
            int r1 = 0, g1 = 0, b1 = 0, r2 = 0, g2 = 0, b2 = 0, 
                i = 0, ix = bufferIndex, count = end;

            while(i < end) {
                r = buffer[ix+2];
                g = buffer[ix+1];
                b = buffer[ix]; 

                if(isGray(r, g, b)) {
                    r1 += r;
                    g1 += g;
                    b1 += b;
                    count--;
                } else {
                    r2 += r;
                    g2 += g;
                    b2 += b;
                }

                ix += 4;
                if(++i % width == 0)
                    ix += padding;
            }

            if(count != 0) {
                color.mean[0] = (byte)(r2 / count);
                color.mean[1] = (byte)(g2 / count);
                color.mean[2] = (byte)(b2 / count);
                color.memberCount = count;
            } else {
                color.mean[0] = 0;
                color.mean[1] = 0;
                color.mean[2] = 0;
                color.memberCount = 0;
            }

            count = end - count;
            if(count != 0) {
                gray.mean[0] = (byte)(r1 / count);
                gray.mean[1] = (byte)(g1 / count);;
                gray.mean[2] = (byte)(b1 / count);;
                gray.memberCount = count;
            } else {
                gray.mean[0] = 0;
                gray.mean[1] = 0;
                gray.mean[2] = 0;
                gray.memberCount = 0;
            }

            centroidMean = Clusters[count == end? 0 : 1].mean;
            mean[0] = centroidMean[0];
            mean[1] = centroidMean[1];
            mean[2] = centroidMean[2];
        }
    }
}