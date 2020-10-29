using System;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;

namespace AyeBlinkin.Serial 
{
    internal partial class SerialCom : IDisposable
    {
        private const int baud = 115200;
        private SerialPort port;
        private CancellationTokenSource writeCancel;
        private CancellationTokenSource readCancel;

        private static bool initialized = false;

        public void Dispose() {
            if(port?.IsOpen??false)
                Write(Message.ExitSerialCom().Raw);

            initialized = false;
            try { writeCancel?.Cancel(); }
            catch(ObjectDisposedException) { }
            try { readCancel?.Cancel(); }
            catch(ObjectDisposedException) {}

            writeCancel?.Dispose();
            readCancel?.Dispose();
            port?.Dispose();
        }

        ~SerialCom() => Dispose();

        public SerialCom() => Initialize();

        private void Initialize() {
            port = new SerialPort(Settings.Model.SerialComId, baud, Parity.None, 8, StopBits.One);
            port.ReadTimeout = -1;
            port.WriteTimeout = -1;
            port.DtrEnable = true;
            port.Encoding = Encoding.GetEncoding(28591); //ISO 8859-1 8bit
            port.Open();

            Settings.Model.PropertyChanged += SendPattern;
            writeCancel = new CancellationTokenSource();
            readCancel = new CancellationTokenSource();
            initialized = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(Reader), readCancel.Token);
            ThreadPool.QueueUserWorkItem(new WaitCallback(Writer), writeCancel.Token);
        }

        private void SendPattern(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName) 
            {
                case nameof(Settings.Model.PatternId):
                    var value = Settings.Model.PatternId;
                    Enqueue(Message.SetPattern(value));
                    
                    if(value == -2) { //manual control
                       Enqueue(Message.SetRed(Settings.Model.Red));
                       Enqueue(Message.SetGreen(Settings.Model.Green));
                       Enqueue(Message.SetBlue(Settings.Model.Blue));
                       Enqueue(Message.SetBright(Settings.Model.Brightness));
                    }
                    break;
                case nameof(Settings.Model.HorizontalLEDs):
                case nameof(Settings.Model.VerticalLEDs):
                    Enqueue(Message.SetLedNumber(Settings.Model.TotalLeds));
                    break;
                case nameof(Settings.Model.Brightness):
                    Enqueue(Message.SetBright(Settings.Model.Brightness));
                    break;
                case nameof(Settings.Model.Red):
                    Enqueue(Message.SetRed(Settings.Model.Red));
                    break;
                case nameof(Settings.Model.Green):
                    Enqueue(Message.SetGreen(Settings.Model.Green));
                    break;
                case nameof(Settings.Model.Blue):
                    Enqueue(Message.SetBlue(Settings.Model.Blue));
                    break;
                case nameof(Settings.Model.Mirror):
                    Enqueue(Settings.Model.Mirror? Message.StreamStart() : Message.StreamEnd());
                    break;
            }
        }

    }
}