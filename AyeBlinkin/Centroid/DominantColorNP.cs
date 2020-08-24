using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid 
{
    internal class DominantColorNP 
    {
        internal class Centroid : CentroidBase 
        {
            public int previousMemberCount;
            public readonly int[] previousmean = new int[3];
           
            public Centroid() { }
            public Centroid(int r, int g, int b) { 
                mean[0] = r;
                mean[1] = g;
                mean[2] = b;
            }

            public void Recenter() {
                if(members.Count == 0) return;

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
            }
        }

        private static int distance(int[] a, int[] z) {
            var r = z[0]-a[0];
            var g = z[1]-a[1];
            var b = z[2]-a[2];
            return r*r + g*g + b*b;
        }

        private const int grayThreshold = 30;

        private class initialKs {
            public readonly Centroid[] Ks;
            private readonly int length;

            public void Recenter() {
                for(var i = 0; i < length; i++)
                    Ks[i].Recenter();
            }

            public bool CentersChanged() {
                Centroid c;
                for(var i = 0; i < length; i++) {
                    c = Ks[i];
                    if(c.memberCount == 0)
                        continue;
                    if(c.memberCount != c.previousMemberCount 
                        || c.mean[0] != c.previousmean[0]
                        || c.mean[1] != c.previousmean[1]
                        || c.mean[2] != c.previousmean[2])
                        return true;
                }
                return false;
            }

            public initialKs() 
            {
                length = 5;
                Ks = new Centroid[5];
                
                Ks[0] = new Centroid(60, 60, 60); //grays
                Ks[1] = new Centroid(150, 60, 60); //r
                Ks[2] = new Centroid(60, 150, 60); //g
                Ks[3] = new Centroid(60, 60, 150); //b
                Ks[4] = new Centroid(60, 150, 150); //c
                //Ks[5] = new Centroid(150, 60, 150); //m
                //Ks[6] = new Centroid(150, 150, 60); //y
            }
        }

        public static byte[] Calculate(ref byte[] memBuffer, ref Rectangle rec, int padding) 
        {
            //var sw = new Stopwatch();
            //sw.Start();

            int key, r, g, b, x = 0, 
                width = rec.Width, height = rec.Height,
                end = width * height, ix = width * 4 * rec.Top + (rec.Left * 4);

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

                key = r << 16 | g << 8 | b;

                if(groups.ContainsKey(key))
                    groups[key].Add(mean);
                else
                    groups.Add(key, new List<int[]>() { mean });

                ix += 4;
                if(++x % width == 0)
                    ix += padding;
            }

            //build clusters
            end = init.Ks.Length;
            r = 10;
            do {
                foreach(var k in groups.Values) 
                {
                    mean = k[0];

                    if(Math.Abs(mean[0]-mean[1]) <= grayThreshold 
                        && Math.Abs(mean[0]-mean[2]) <= grayThreshold 
                        && Math.Abs(mean[2]-mean[1]) <= grayThreshold) {
                        init.Ks[0].members.AddRange(k);
                        continue;
                    }

                    for(x = 1, key = int.MaxValue; x < end; x++) 
                    {
                        ix = distance(init.Ks[x].mean, mean);
                        if(ix < key) {
                            key = ix;
                            target = init.Ks[x];
                        }
                    }
                    target.members.AddRange(k);
                }
                
                init.Recenter();
            }
            while(r-- > 0 && init.CentersChanged());

            //get dominant cluster
            for(x = 1, key = 0, mean = init.Ks[0].mean, end = init.Ks.Length; x < end; x++) 
            {
                ix = init.Ks[x].memberCount;
                if(key < ix) {
                    key = ix;
                    mean = init.Ks[x].mean;
                }
            }

            //rgb must be byte in range 0-254
            result = new byte[] { (byte)mean[0], (byte)mean[1], (byte)mean[2] };
            if(result[0] == 0xFF) result[0] = 0xFE;
            if(result[1] == 0xFF) result[1] = 0xFE;
            if(result[2] == 0xFF) result[2] = 0xFE;

            //Console.WriteLine(sw.Elapsed.TotalMilliseconds);

            if(rec.X == 0 && rec.Y == 0) {
                DominantColorForm.setBackground(ref memBuffer, ref rec, padding);
                DominantColorForm.setColors(init.Ks);
            }

            return result;
        }
    }
}