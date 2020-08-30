using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AyeBlinkin.Serial 
{


    internal partial class SerialCom
    {
        private bool readerStarted = false;
        private void Reader(object obj) {
            Thread.CurrentThread.Name = $"RX Message Loop";
            var token = (CancellationToken)obj;

            var messageBuffer = new List<byte>();
            var readBuffer = new byte[1024];
            readerStarted = true;

            while(!token.IsCancellationRequested) 
            {
                try {
                    Task.Run(() => {
                        try { messageBuffer.AddRange(readBuffer.Take(port.Read(readBuffer, 0, 1024))); }
                        catch (Exception) { }
                    }).Wait(token);
                }
                catch(OperationCanceledException) {
                    port.ReadTimeout = 1;
                }

                if(token.IsCancellationRequested)
                    break;

                while(messageBuffer.Count > 0) 
                {
                    if(messageBuffer[0] == Message.INTERRUPT) 
                    {
                        if(messageBuffer.Count < 4) // INTERRUPT, COMMAND, VALUE(s), INTERRUPT
                            break;

                        var ix = messageBuffer.IndexOf(Message.INTERRUPT, 3);
                        if(ix < 0)
                            break;

                        ProcessMessage(messageBuffer.Skip(1).Take(ix-1));
                        messageBuffer = messageBuffer.Skip(ix + 1).ToList();
                    } 
                    else 
                        messageBuffer.RemoveAt(0);
                }
            }
        }

        private enum Command : byte {
            Patterns    = 0xFE,
            Brightness  = 0xFD,
            Pattern     = 0xFC,
            Continue    = 0xFB
        }

        private void ProcessMessage(IEnumerable<byte> message) 
        {
            switch(message.ElementAt(0)) {
                case(byte)Command.Continue:
                    XON = true;
                    break;

                case (byte)Command.Patterns:
                    var msg = new String(message.Skip(1).Select(Convert.ToChar).ToArray());
                    Settings.Model.Patterns = msg.Substring(0, msg.Length-1)
                        .Split('\r')
                        .Select((s, x) => new {s, x})
                        .ToDictionary(x => x.x, x => x.s);
                    break;

                case (byte)Command.Pattern: 
                    Settings.Model.PatternId = (int)message.ElementAt(1);
                    break;

                case (byte)Command.Brightness:
                    Settings.Model.Brightness = (int)message.ElementAt(1);
                    break;
            }
        }
    }
}