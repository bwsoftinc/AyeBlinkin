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
        private static CancellationTokenSource soundThreadCancel;
        private static CancellationTokenSource screenThreadCancel;
        private static readonly WaitCallback com = new WaitCallback(SerialCom.Run);
        private static readonly WaitCallback sound = new WaitCallback(HardwareSoundCapture.Run);
        private static readonly WaitCallback screen = new WaitCallback(HardwareScreenCapture.Run);
        internal static string Name { get; } = Assembly.GetExecutingAssembly().GetName().Name;

        [STAThread]
        static void Main() 
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(OnExit);

            //StartStopAudioThread();
            StartStopScreenThread();
            Application.Run(trayApp = new AyeBlinkinTray());
        }

        internal static void StartStopComThread() => StartStop(!string.IsNullOrWhiteSpace(Settings.Model.SerialComId), com, ref comThreadCancel);
        internal static void StartStopScreenThread() => StartStop(Settings.Model.Mirror, screen, ref screenThreadCancel);
        internal static void StartStopAudioThread() => StartStop(Settings.Model.Audio, sound, ref soundThreadCancel);
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
            try { soundThreadCancel?.Cancel(); } catch { }
            
            screenThreadCancel?.Dispose();
            comThreadCancel?.Dispose();
            soundThreadCancel?.Dispose();

            Settings.SaveToDisk();
            trayApp.hideIcon();
        }
    }
}
