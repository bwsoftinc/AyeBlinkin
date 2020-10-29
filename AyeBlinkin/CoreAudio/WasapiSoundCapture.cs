using System;
using System.Threading;

using NAudio.Wave;
using AyeBlinkin.Serial;

namespace AyeBlinkin.CoreAudio 
{
    internal class WasapiSoundCapture : IDisposable
    {
        private static WasapiSoundCapture instance;
        private WasapiLoopbackCapture capture = new WasapiLoopbackCapture();
        private int bytesPerSample;

        ~WasapiSoundCapture() => this.Dispose();

        static WasapiSoundCapture() 
        {
            Settings.Model.PropertyChanged += (s, e) => 
            {
                if(e.PropertyName == nameof(Settings.Model.Audio))
                {
                    if(Settings.Model.Audio)
                        WasapiSoundCapture.Start();
                    else
                        WasapiSoundCapture.Stop();
                }
            };
        }

        private WasapiSoundCapture() 
        {
            bytesPerSample = capture.WaveFormat.BitsPerSample / 8;
            capture.DataAvailable += DataAvailable;
            capture.RecordingStopped += (s, a) => Dispose();
        }

        private void DataAvailable(object sender, WaveInEventArgs e) 
        {
            var peak = MaxFloat(e.Buffer, e.BytesRecorded) * 100F;
            var val = peak <= 2F? 38F : 255F * Math.Log10(peak) / 2F;
            SerialCom.Enqueue(Message.SetBright((int)val));
            Thread.Sleep(33);   
        }

        private float MaxFloat(byte[] buffer, int length) 
        {
            float max = 0F, next = 0F;
            for(var i = 0; i < length; i += bytesPerSample) 
            {
                next = Math.Abs(BitConverter.ToSingle(buffer, i));
                if(next > max)
                    max = next;
            }

            return max;
        }

        internal static void Stop() 
        {
            if(instance == null)
                return;

            instance.capture.StopRecording();
            instance = null;
            SerialCom.Enqueue(Message.SetBright(Settings.Model.Brightness));
        }

        internal static void Start() 
        {
            if(instance == null)
                instance = new WasapiSoundCapture();
            
            instance.capture.StartRecording();
        }

        public void Dispose() 
        {
            capture.Dispose();
        }
    }
}
