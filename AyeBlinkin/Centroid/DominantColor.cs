//#undef DEBUG
using System;
using System.Drawing;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid
{
    internal class DominantColor : ICentroidColor
    {
        internal class Centroid : CentroidBase 
        {
            public int previousMemberCount;
            public readonly int[] previousmean = new int[3];

            public Centroid(int r, int g, int b) { 
                mean[0] = r;
                mean[1] = g;
                mean[2] = b;
            }

            public bool Recenter() {
                if(members.Count == 0) return false;

                int i = 0, count = members.Count;
                int[] c, totals = new int[3];

                previousmean[0] = mean[0];
                previousmean[1] = mean[1];
                previousmean[2] = mean[2];

                for(;i < count;i++) {
                    c = members[i];
                    totals[0] += c[0];
                    totals[1] += c[1];
                    totals[2] += c[2];
                }
                
                mean[0] =  totals[0] / count;
                mean[1] =  totals[1] / count;
                mean[2] =  totals[2] / count;

                previousMemberCount = memberCount;
                memberCount = count;
                members.Clear();

                return previousMemberCount != memberCount
                    || previousmean[0] != mean[0]
                    || previousmean[1] != mean[1]
                    || previousmean[2] != mean[2];
            }
        }

        private static int distance(int[] a, int[] z)
        {
            var rmean = (a[0] + z[0]) / 2;
            var r = a[0] - z[0];
            var g = a[1] - z[1];
            var b = a[2] - z[2];
            return (((512+rmean)*r*r)>>8) + 4*g*g + (((767-rmean)*b*b)>>8);
        }

        private static int euclidDistance(int[] a, int[] z) 
        {
            var r = z[0]-a[0];
            var g = z[1]-a[1];
            var b = z[2]-a[2];
            return r*r + g*g + b*b;
        }

        private class initialKs {
            public readonly Centroid[] Ks;
            private readonly int length;
            public bool Recenter() 
            {
                var changed = false;
                for(var i = startCluster; i < length; i++)
                    changed |= Ks[i].Recenter();

                return changed;
            }

            public initialKs() 
            {
                length = 4;
                Ks = new Centroid[length];
                
                Ks[0] = new Centroid(60, 60, 60); //grays

                Ks[1] = new Centroid(180, 60, 60); //r
                Ks[2] = new Centroid(60, 180, 60); //g
                Ks[3] = new Centroid(60, 60, 180); //b
                //Ks[4] = new Centroid(60, 180, 180); //c
                //Ks[5] = new Centroid(180, 60, 180); //m
                //Ks[6] = new Centroid(180, 180, 60); //y
            }
        }

        private const int startCluster = 1;
        private const int grayThreshold = 10;

        public byte[] Calculate(ref byte[] memBuffer, ref Rectangle[] recs, int recindex, int scan) 
        {
            if(recindex >= recs.Length)
                return new byte[0];

            var rec = recs[recindex];
            int key, r, g, b, x = 0, padding = 4 * (scan - rec.Width),
                width = rec.Width, height = rec.Height,
                end = width * height, ix = (((width * 4) + padding) * rec.Y) + (rec.X * 4);

            byte[] result = null;
            int[] mean = null;
            Centroid target = null;
            var init = new initialKs();
            var groups = new Dictionary<int, List<int[]>>();

            //initialize clusters
            while(x < end) {
                mean = new int[3] {
                    r = memBuffer[ix+2],
                    g = memBuffer[ix+1],
                    b = memBuffer[ix] 
                };

                if(Math.Abs(r-g) <= grayThreshold 
                    && Math.Abs(g-b) <= grayThreshold 
                    && Math.Abs(r-b) <= grayThreshold) 
                {
                    init.Ks[0].members.Add(mean);
                } 
                else 
                {
                    key = r << 16 | g << 8 | b;

                    if(groups.ContainsKey(key))
                        groups[key].Add(mean);
                    else
                        groups.Add(key, new List<int[]>() { mean });
                }

                ix += 4;
                if(++x % width == 0)
                    ix += padding;
            }

            init.Ks[0].Recenter();

            //build clusters
            end = init.Ks.Length;
            r = 10;
            do {
                foreach(var k in groups.Values) 
                {
                    mean = k[0];

                    for(x = startCluster, key = int.MaxValue; x < end; x++) 
                    {
                        ix = euclidDistance(init.Ks[x].mean, mean);
                        if(ix < key) {
                            key = ix;
                            target = init.Ks[x];
                        }
                    }
                    target.members.AddRange(k);
                }
            }
            while(init.Recenter() && r-- > 0);

            //get 1, 2 dominant clusters
            for(x = startCluster, r = 0, key = 0, g = init.Ks[key].memberCount, end = init.Ks.Length; x < end; x++) 
            {
                b = init.Ks[x].memberCount; 
                if(b == 0)
                    continue;
                if(key == 0 || b > g) {
                    r = key;
                    key = x;
                    g = init.Ks[key].memberCount;
                }
            }

            mean = init.Ks[key].mean;
            //reduce flicker for dominance rivalry
            /*if(r != 0 && (Math.Abs(init.Ks[r].memberCount - g) < 40 || init.Ks[r].memberCount + g < width * height / 2)) {
                init.Ks[r].mean[0] = (mean[0] + init.Ks[r].mean[0]) / 2;
                init.Ks[r].mean[1] = (mean[1] + init.Ks[r].mean[1]) / 2;
                init.Ks[r].mean[2] = (mean[2] + init.Ks[r].mean[2]) / 2;
                mean[0] = init.Ks[r].mean[0];
                mean[1] = init.Ks[r].mean[1];
                mean[2] = init.Ks[r].mean[2];
            }
            */
            
            //integrate gray cluster at 1/3 weight
            //mean[0] = (mean[0] * 2 + init.Ks[0].mean[0]) / 3;
            //mean[1] = (mean[1] * 2 + init.Ks[0].mean[1]) / 3;
            //mean[2] = (mean[2] * 2 + init.Ks[0].mean[2]) / 3;

            //rgb must be byte in range 0-254
            result = new byte[] { (byte)mean[0], (byte)mean[1], (byte)mean[2] };
            if(result[0] == 0xFF) result[0] = 0xFE;
            if(result[1] == 0xFF) result[1] = 0xFE;
            if(result[2] == 0xFF) result[2] = 0xFE;

#if DEBUG
            if(recindex == Settings.Model.PreviewLED) {
                CentroidColorForm.setBackground(ref memBuffer, ref rec, padding);
                CentroidColorForm.setColors(init.Ks);
            }
#endif
            return result;
        }
    }
}