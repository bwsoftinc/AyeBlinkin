using System;
using System.Linq;
using System.Threading;
using SharpDX.Multimedia;
using SharpDX.DirectSound;

namespace AyeBlinkin.DirectX
{
    internal class HardwareSoundCapture : IDisposable 
    {
        private const int sampleCount = 294;
        private const double scale = (double)short.MaxValue / 100D;
        private WaveFormat format;
        private CaptureBuffer buffer;
        private DirectSoundCapture device;
        private CaptureBufferDescription description;

        private HardwareSoundCapture() 
        {
            format = new WaveFormat(44100, 16, 1);
            description = new CaptureBufferDescription() {
                Format = format,
                BufferBytes = format.AverageBytesPerSecond,
                Flags = CaptureBufferCapabilitiesFlags.WaveMapped
            };

            //Stereo Mix -> realtek loopback audio device
            var guid = DirectSoundCapture.GetDevices().FirstOrDefault(x=> x.Description.StartsWith("Stereo Mix"))?.DriverGuid ?? Guid.Empty;
            device = new DirectSoundCapture(guid);
            buffer = new CaptureBuffer(device, description);

            buffer.SetNotificationPositions(new[] { new NotificationPosition() {
                Offset = buffer.Capabilities.BufferBytes - 1,
                WaitHandle = new AutoResetEvent(false)
            }});
        }

        public void Dispose() {
            buffer?.Stop();
            buffer?.Dispose();
            device?.Dispose();
        }

        ~HardwareSoundCapture() => Dispose();

        internal static void Run(object obj) {
            Thread.CurrentThread.Name = "DirectSound Capture";
            var token = (CancellationToken)obj;

            var samples = new short[sampleCount];
            var instance = new HardwareSoundCapture();
            instance.buffer.Start(true);

            while(true)
            {
                try {
                    instance.buffer.Read<short>(samples, 0, sampleCount, instance.buffer.CurrentRealPosition, LockFlags.None);
                    var peak = samples.Select(x => Math.Abs((double)x)).Max() / scale;
                    //var rms = Math.Sqrt(samples.Select(x => (double)x * (double)x).Sum() / sampleCount);
                    var val = peak <= 1d? 0d : 255d * Math.Log10(peak) / 2d;
                    Settings.Model.Brightness = (int)val;
                } catch (Exception) {

                }

                //Console.WriteLine($"{peak:00.00} {val:00}");
                Thread.Sleep(33);

                if(token.IsCancellationRequested) {
                    instance.Dispose();
                    break;
                }
            }
        }
    }
}