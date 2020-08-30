using System.Drawing;

namespace AyeBlinkin.Centroid
{
    internal interface ICentroidColor 
    {
        byte[] Calculate(ref byte[] memBuffer, ref Rectangle[] recs, int recindex, int scan);
    }
}