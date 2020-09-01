using System;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

using AyeBlinkin.Forms;
using AyeBlinkin.Serial;
using AyeBlinkin.DirectX;
using AyeBlinkin.Centroid;

namespace AyeBlinkin 
{
    static class AyeBlinkin 
    {
        private static AyeBlinkinTray trayApp;
        private static CancellationTokenSource comThreadCancel;
        private static CancellationTokenSource screenThreadCancel;
        private static readonly WaitCallback com;
        private static readonly WaitCallback screen;
        internal static string Name { get; } = Assembly.GetExecutingAssembly().GetName().Name;

        static AyeBlinkin() {
            com = new WaitCallback(SerialCom.Run);
            screen = new WaitCallback(HardwareScreenCapture<ClassifiedColor>.Run);
        }

        [STAThread]
        static void Main() 
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(OnExit);

            StartStopScreenThread();
            Application.Run(trayApp = new AyeBlinkinTray());
        }

        internal static void StartStopComThread() => StartStop(!string.IsNullOrWhiteSpace(Settings.Model.SerialComId), com, ref comThreadCancel);
        internal static void StartStopScreenThread() => StartStop(Settings.Model.Mirror, screen, ref screenThreadCancel);
        private static void StartStop(bool start, WaitCallback job, ref CancellationTokenSource cancel) 
        {            
            if(!start) 
            {
                try { cancel?.Cancel(); } catch { }
                cancel?.Dispose();
                return;
            }

            if(!cancel?.IsCancellationRequested??false)
                try { cancel?.Cancel(); } catch { }

            cancel?.Dispose();
            cancel = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(job, cancel.Token);
        }

        private static void OnExit(object sender, EventArgs e) 
        {
            Settings.SettingsHwnd = IntPtr.Zero;

            try { screenThreadCancel?.Cancel(); } catch { }
            try { comThreadCancel?.Cancel(); } catch { }
            
            screenThreadCancel?.Dispose();
            comThreadCancel?.Dispose();

            Settings.SaveToDisk();
            trayApp.hideIcon();
        }
    }
}
