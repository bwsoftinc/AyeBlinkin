using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace AyeBlinkin.Serial 
{
    internal partial class SerialCom : IDisposable
    {
        private const int maxPacketSize = 48;
        private static object enqueueLock = new object();
        private static Queue<Message.Command> MessageQueue = new Queue<Message.Command>();

        internal static void Enqueue(Message.Command item) { 
            lock(enqueueLock) 
                MessageQueue.Enqueue(item);
        }

        private void Write(byte[] bytes) => port.Write(bytes, 0, bytes.Length);

        internal static void Run(object obj) {
            Thread.CurrentThread.Name = "TX Message Loop";
            var token = (CancellationToken)obj;
            
            SerialCom instance = null;
            while(!token.IsCancellationRequested) 
            {
                try
                {
                    if(instance == null)
                    {
                        instance = new SerialCom();
                        instance.Initialize();
                    }

                    if(instance.XON && MessageQueue.Count > 0) 
                    {
                        var item = MessageQueue.Dequeue();
                        if(!MessageQueue.Any(x => x.Type == item.Type)) 
                        {
                            if(item.Type == Message.Type.Stream) 
                            {
                                for(var x = 0; x < item.Raw.Length; x += maxPacketSize) 
                                {
                                    instance.port.Write(item.Raw, x, Math.Min(maxPacketSize, item.Raw.Length - x));
                                    while(!instance.XON)
                                        Thread.Sleep(0);
                                }
                            }
                            else 
                                instance.Write(item.Raw);
                        }
#if DEBUG
                        else
                            Console.WriteLine($"{item.Type.ToString()} Message Dropped");
#endif
                    }

                    Thread.Sleep(1);
                }
                catch (Exception)
                {
                    instance?.Dispose();
                    instance= null;
                }
            }

            instance?.Dispose();
        }
    }
}