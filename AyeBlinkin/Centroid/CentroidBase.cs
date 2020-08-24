using System.Collections.Generic;

namespace AyeBlinkin.Centroid {
    internal class CentroidBase {
        public int memberCount;
        public readonly int[] mean = new int[3];
        public readonly List<int[]> members = new List<int[]>();
    }
}