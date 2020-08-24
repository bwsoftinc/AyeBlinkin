using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid {
    internal class DominantColorClassify {
        internal class Centroid : CentroidBase {
            public int sum;
            public readonly int[] totals = new int[3];
            public Centroid() { }
            public Centroid(int r, int g, int b, int t) {
                mean[0] = r;
                mean[1] = g;
                mean[2] = b;
                sum = t;                
            }
            public Centroid(int[] a) {
                mean[0] = a[0];
                mean[1] = a[1];
                mean[2] = a[2];
                members.Add(a);
            }

            public void Add(List<int[]> values) {
                int i = 0, count = values.Count, d = members.Count + count;
                int[] c;

                for(;i < count;i++) {
                    c = values[i];
                    totals[0] += c[0];
                    totals[1] += c[1];
                    totals[2] += c[2];
                }
                
                mean[0] =  totals[0] / d;
                mean[1] =  totals[1] / d;
                mean[2] =  totals[2] / d;

                memberCount = d;
                sum = mean[0] + mean[1] + mean[2];
                members.AddRange(values);
            }
        }

        private static int distance(int[] a, int[] z) {
            var r = z[0]-a[0];
            var g = z[1]-a[1];
            var b = z[2]-a[2];
            return r*r + g*g + b*b;
        }

        private class initialKs {
            public Centroid[] Ks = new Centroid[8];
            
            public initialKs() 
            {
                Ks[0] = new Centroid(); //black
                Ks[1] = new Centroid(); //r
                Ks[2] = new Centroid(); //g
                Ks[3] = new Centroid(); //b
                Ks[4] = new Centroid(); //white
                Ks[5] = new Centroid(); //cyan
                Ks[6] = new Centroid(); //magenta
                Ks[7] = new Centroid(); //yellow
            }

            public void scan(int r, int g, int b) {
                int sum = r + g + b, minsum = Ks[0].sum, maxsum = Ks[3].sum;
                int[] min = Ks[0].mean, red = Ks[1].mean, green = Ks[2].mean, blue = Ks[3].mean, max = Ks[3].mean;

                if(sum < minsum) {
                    min[0] = r;
                    min[1] = g;
                    min[2] = b;
                    Ks[0].sum = sum;
                }
                
                if(sum > maxsum) {
                    max[0] = r;
                    max[1] = g;
                    max[2] = b;
                    Ks[3].sum = sum;
                }

                if(r > red[0]) {
                    red[0] = r;
                    red[1] = g;
                    red[2] = b;
                }

                if(g > green[1]) {
                    green[0] = r;
                    green[1] = g;
                    green[2] = b;
                }

                if(b > blue[2]) {
                    blue[0] = r;
                    blue[1] = g;
                    blue[2] = b;
                }
            }
        }

        public static byte[] Calculate(ref byte[] memBuffer, ref Rectangle rec, int padding) 
        {
            var sw = new Stopwatch();
            sw.Start();

            int key, r, g, b, x = 0, 
                width = rec.Width, height = rec.Height,
                end = width * height, ix = width * 4 * rec.Top + (rec.Left * 4);

            byte[] result = null;
            int[] mean = null;
            var init = new initialKs();
            var groups = new Dictionary<int, List<int[]>>();

            //initialize clusters
            while(x++ < end) {
                mean = new int[3] {
                    r = memBuffer[ix+2],
                    g = memBuffer[ix+1],
                    b = memBuffer[ix] 
                };

                key = r << 16 | g << 8 | b;

                if(groups.ContainsKey(key))
                    groups[key].Add(mean);
                else {
                    init.scan(r, g, b);
                    groups.Add(key, new List<int[]>() { mean });
                }

                ix += 4;
                if(x % width == 0)
                    ix += padding;
            }

            //build clusters
            end = init.Ks.Length;
            foreach(var k in groups.Values) 
            {
                mean = k[0];
                if(mean[0] <= 70 && mean[1] <= 70 && mean[2] <= 70) { //black
                    init.Ks[0].Add(k);
                    continue;
                } else if (mean[0] >= 220 && mean[1] >= 220 && mean[2] >= 220) { //white
                    init.Ks[4].Add(k);
                    continue;
                } else if (mean[2] > mean[1] + 45 && mean[2] > mean[0] + 45) { //blue
                    init.Ks[3].Add(k);
                    continue;
                } else if (mean[1] > mean[0] + 45 && mean[1] > mean[2] + 45) { //green
                    init.Ks[2].Add(k);
                    continue;
                } else if (mean[0] > mean[1] + 45 && mean[0] > mean[2] + 45) { //red
                    init.Ks[1].Add(k);
                    continue;
                } else if (mean[2] > mean[0] + 45 && mean[1] > mean[0] + 45) { //cyan
                    init.Ks[5].Add(k);
                    continue;
                } else if (mean[0] > mean[1] + 45 && mean[2] > mean[1] + 45) { //magenta
                    init.Ks[6].Add(k);
                    continue;
                } else if (mean[0] > mean[2] + 45 && mean[1] > mean[2] + 45) { //yellow
                    init.Ks[7].Add(k);
                    continue;
                }
            }

            //get dominant cluster
            for(x = 0, key = 0, end = init.Ks.Length; x < end; x++) {
                ix = init.Ks[x].members.Count;
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

            Console.WriteLine(sw.Elapsed.TotalMilliseconds);


            if(rec.X == 0 && rec.Y == 0) {
                DominantColorForm.setBackground(ref memBuffer, ref rec, padding);
                DominantColorForm.setColors(init.Ks);
            }

            return result;
        }
    }
}