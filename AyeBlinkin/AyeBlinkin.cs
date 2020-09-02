using System;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

using AyeBlinkin.Forms;
using AyeBlinkin.Serial;
using AyeBlinkin.DirectX;
using AyeBlinkin.Centroid;
using Message = AyeBlinkin.Serial.Message;

namespace AyeBlinkin 
{
    static class AyeBlinkin 
    {
        internal static string Name { get; } = Assembly.GetExecutingAssembly().GetName().Name;
        private static SerialCom serial;
        private static AyeBlinkinTray trayApp;
        private static readonly WaitCallback screen;
        private static CancellationTokenSource screenThreadCancel;
        static AyeBlinkin() => screen = new WaitCallback(HardwareScreenCapture<ClassifiedColor>.Run);

        [STAThread]
        static void Main() 
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(OnExit);
            Application.Run(trayApp = new AyeBlinkinTray());
        }

        internal static void StartStopComThread() 
        {
            serial?.Dispose();
            if(!string.IsNullOrWhiteSpace(Settings.ComPort))
            {
                serial = new SerialCom();
                SerialCom.Enqueue(Message.SetLedNumber(Settings.TotalLeds));
                SerialCom.Enqueue(Message.GetPatterns());
            }
        }
        
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
            screenThreadCancel?.Dispose();
            serial?.Dispose();

            Settings.SaveToDisk();
            trayApp.hideIcon();
        }
    }
}
