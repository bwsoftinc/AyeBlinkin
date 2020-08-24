using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid {
    internal class DominantColorMaxInit {
        internal class Centroid : CentroidBase {
            public int sum;
            public readonly int[] totals = new int[3];
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
            public Centroid[] Ks = new Centroid[4];

            public initialKs() 
            {
                Ks[0] = new Centroid(255, 255, 255, 765); //black
                Ks[1] = new Centroid(0, 255, 255, 510); //r
                Ks[2] = new Centroid(255, 0, 255, 510); //g
                Ks[3] = new Centroid(255, 255, 0, 510); //b
            }

            public void scan(int r, int g, int b) {
                int sum = r + g + b, minsum = Ks[0].sum;
                int[] min = Ks[0].mean, red = Ks[1].mean, green = Ks[2].mean, blue = Ks[3].mean;

                if(sum < minsum) {
                    min[0] = r;
                    min[1] = g;
                    min[2] = b;
                    Ks[0].sum = sum;
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
            Centroid target = null;
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
                for(x = 0, mean = k[0], key = int.MaxValue; x < end; x++) 
                {
                    ix = distance(init.Ks[x].mean, mean);
                    if(ix < key) {
                        key = ix;
                        target = init.Ks[x];
                    }
                }
                target.Add(k);
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

            init.Ks.OrderByDescending(z => z.members.Count).ToList()
                .ForEach(z => Console.WriteLine($"{100*z.members.Count/(width * height)} {z.mean[0]} {z.mean[1]} {z.mean[2]}"));

            return result;
        }
    }
}