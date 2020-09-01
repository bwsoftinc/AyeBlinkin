using System.Collections.Generic;

namespace AyeBlinkin.Centroid {
    internal class CentroidColor {
        private byte r;
        private byte g;
        private byte b;
        public int memberCount;
        public readonly byte[] mean = new byte[3];
        public readonly List<byte[]> members = new List<byte[]>();
        public int previousMemberCount;
        public readonly byte[] previousmean = new byte[3];

        public CentroidColor() { }
        public CentroidColor(byte r, byte g, byte b) { 
            this.r = r;
            this.g = g;
            this.b = b;
            mean[0] = r;
            mean[1] = g;
            mean[2] = b;
        }

        public void Reset() {
            mean[0] = r;
            mean[1] = g;
            mean[2] = b;
            previousmean[0] = 0;
            previousmean[1] = 0;
            previousmean[2] = 0;
            members.Clear();
            memberCount = 0;
            previousMemberCount = 0;
        }

        public bool Recenter() {
            if(members.Count == 0)
                return false;

            int i = 0, count = members.Count;
            byte[] c;
            var totals = new int[3];

            for(;i < count; i++) {
                c = members[i];
                totals[0] += c[0];
                totals[1] += c[1];
                totals[2] += c[2];
            }

            previousmean[0] = mean[0];
            previousmean[1] = mean[1];
            previousmean[2] = mean[2];
            
            mean[0] =  (byte)(totals[0] / count);
            mean[1] =  (byte)(totals[1] / count);
            mean[2] =  (byte)(totals[2] / count);

            previousMemberCount = memberCount;
            memberCount = count;
            members.Clear();

            return previousMemberCount != memberCount
                || previousmean[0] != mean[0]
                || previousmean[1] != mean[1]
                || previousmean[2] != mean[2];
        }
    }
}