using System;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid
{
    internal class DominantColor : CentroidBase
    {
        private const int startCluster = 1;
        public DominantColor()
        {
            Clusters = new CentroidColor[4] 
            {
                new CentroidColor(60, 60, 60), //grays
                new CentroidColor(180, 60, 60), //r
                new CentroidColor(60, 180, 60), //g
                new CentroidColor(60, 60, 180), //b
            };
        }

        private void Reset() 
        {
            for(var i = 0; i < Clusters.Length; i++) 
                Clusters[i].Reset();
        }

        public bool Recenter() 
        {
            var changed = false;
            for(var i = startCluster; i < Clusters.Length; i++)
                changed |= Clusters[i].Recenter();

            return changed;
        }

        private static int distance(byte[] a, byte[] z)
        {
            var rmean = ((int)a[0] + (int)z[0]) / 2;
            var r = (int)a[0] - (int)z[0];
            var g = (int)a[1] - (int)z[1];
            var b = (int)a[2] - (int)z[2];
            return (((512+rmean)*r*r)>>8) + 4*g*g + (((767-rmean)*b*b)>>8);
        }

        private static int euclidDistance(byte[] a, byte[] z) 
        {
            var r = (int)z[0]-(int)a[0];
            var g = (int)z[1]-(int)a[1];
            var b = (int)z[2]-(int)a[2];
            return r*r + g*g + b*b;
        }

        protected override void Implementation() 
        {
            byte r, g, b;
            int key, i = 0, ix = bufferIndex;

            byte[] color = null;
            CentroidColor target = null;
            var groups = new Dictionary<int, List<byte[]>>();

            Reset();
            while(i < end) 
            {
                color = new byte[3] 
                {
                    r = buffer[ix+2],
                    g = buffer[ix+1],
                    b = buffer[ix] 
                };

                if(isGray(r, g, b))
                    Clusters[0].members.Add(color);
                else 
                {
                    key = (int)r << 16 | (int)g << 8 | (int)b;

                    if(groups.ContainsKey(key))
                        groups[key].Add(color);
                    else
                        groups.Add(key, new List<byte[]>() { color });
                }

                ix += 4;
                if(++i % width == 0)
                    ix += padding;
            }

            Clusters[0].Recenter();

            //build clusters
            r = 4;
            do {
                foreach(var k in groups.Values) 
                {
                    color = k[0];

                    for(i = startCluster, key = int.MaxValue; i < Clusters.Length; i++) 
                    {
                        ix = euclidDistance(Clusters[i].mean, color);
                        if(ix < key)
                        {
                            key = ix;
                            target = Clusters[i];
                        }
                    }
                    target.members.AddRange(k);
                }
            }
            while(Recenter() && --r > 0);

            //get 1, 2 dominant clusters
            for(g = startCluster, r = 0, b = 0, key = 0, ix = Clusters.Length; g < ix; g++)
            {
                i = Clusters[g].memberCount; 
                if(i == 0)
                    continue;
                if(i > key)
                {
                    r = b;
                    b = g;
                    key = Clusters[b].memberCount;
                }
            }

            color = Clusters[b].mean;

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

            mean[0] = color[0];
            mean[1] = color[1];
            mean[2] = color[2];
        }
    }
}