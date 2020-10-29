using System.Drawing;
using System.Reflection;

namespace AyeBlinkin.Resources 
{
    internal static class Icons 
    {
        internal static Icon Settings => Get("AyeBlinkin.Resources.Settings.ico");
        internal static Icon Program => Get("AyeBlinkin.Resources.Program.ico");

        private static Icon Get(string path) 
        {
            using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                return new Icon(s);
        }
    }
}