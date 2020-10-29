namespace AyeBlinkin.Centroid
{
    internal class ClassifiedColor : CentroidBase
    {
        private const int startCluster = 1;
        public ClassifiedColor()
        {
            Clusters = new CentroidColor[7]
            {
                new CentroidColor(60, 60, 60), //grays
                new CentroidColor(180, 60, 60), //r
                new CentroidColor(60, 180, 60), //g
                new CentroidColor(60, 60, 180), //b
                new CentroidColor(60, 180, 180), //c
                new CentroidColor(180, 60, 180), //m
                new CentroidColor(180, 180, 60), //y
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
            for(var i = 0; i < startCluster; i++)
                Clusters[i].Recenter();

            for(var i = startCluster; i < Clusters.Length; i++)
                changed |= Clusters[i].Recenter();

            return changed;
        }
        
        protected override void Implementation() 
        {
            byte r, g, b;
            int key = 0, j = 0, dist = 0, i = 0, ix = bufferIndex;

            byte[] color = null;
            CentroidColor target = null;

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
                    for(j = startCluster, key = int.MaxValue; j < Clusters.Length; j++) 
                    {
                        dist = euclidDistance(Clusters[j].mean, color);
                        if(dist < key) 
                        {
                            key = dist;
                            target = Clusters[j];
                        }
                    }
                    target.members.Add(color);
                }

                ix += 4;
                if(++i % width == 0)
                    ix += padding;
            }

            Recenter();

            //get 1, 2 dominant clusters at indexes b, r
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
            mean[0] = color[0];
            mean[1] = color[1];
            mean[2] = color[2];
        }
    }
}