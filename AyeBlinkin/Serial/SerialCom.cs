using System;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using System.Management;
using System.ComponentModel;
using System.Collections.Generic;

namespace AyeBlinkin.Serial 
{
    internal partial class SerialCom : IDisposable
    {
        private const int maxTX = 192;
        private volatile int remoteBufferLeft = maxTX;
        private volatile bool XON = false;
        private SerialPort port;
        private CancellationTokenSource readCancel = new CancellationTokenSource();

        public void Dispose() {
            if(port?.IsOpen??false)
                Write(Message.ExitSerialCom().Raw);

            readCancel.Cancel();
            port?.Dispose();
        }

        private SerialCom() { 
            port = new SerialPort(Settings.Model.SerialComId, 115200, Parity.None, 8, StopBits.One);
            port.Open();
            port.ReadTimeout = -1;
            port.WriteTimeout = -1;
            port.DtrEnable = true;

            Settings.Model.PropertyChanged += SendPattern;
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Reader), readCancel.Token);
            Write(Message.Clear().Raw);
            Write(Message.GetPatterns().Raw);
        }

        private void SendPattern(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName) 
            {
                case "PatternId":
                    var value = Settings.Model.PatternId;
                    Enqueue(Message.SetPattern(value));
                    
                    if(value == -2) {
                       Enqueue(Message.SetRed(Settings.Model.Red));
                       Enqueue(Message.SetGreen(Settings.Model.Green));
                       Enqueue(Message.SetBlue(Settings.Model.Blue));
                       Enqueue(Message.SetBright(Settings.Model.Brightness));
                    }
                    break;
                case "Brightness":
                    Enqueue(Message.SetBright(Settings.Model.Brightness));
                    break;
                case "Red":
                    Enqueue(Message.SetRed(Settings.Model.Red));
                    break;
                case "Green":
                    Enqueue(Message.SetGreen(Settings.Model.Green));
                    break;
                case "Blue":
                    Enqueue(Message.SetBlue(Settings.Model.Blue));
                    break;
            }
        }

        internal static Dictionary<string, string> GetUsbDevicePorts() 
        {
            var usbs = new List<string>();
            using(var searcher = new ManagementObjectSearcher(@"select Name From Win32_PnPEntity where Name like 'BlinkyTape (%)'"))
            using(var collection = searcher.Get())
                foreach(var obj in collection) {
                    usbs.Add((string)obj.GetPropertyValue("Name"));
                    obj.Dispose();
                }

            var coms = new HashSet<string>(SerialPort.GetPortNames().Select(x => x.ToUpper()));
            return usbs.Select(x => {
                var ix = x.LastIndexOf("(") + 1;
                return new {
                    com = x.Substring(ix, x.Length - ix - 1).ToUpper(),
                    name = x
                };
            }).Where(x => coms.Contains(x.com)).ToDictionary(x => x.com, x => x.name);
        }
    }
}