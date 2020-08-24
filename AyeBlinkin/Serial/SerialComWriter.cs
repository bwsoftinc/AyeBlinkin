using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace AyeBlinkin.Serial 
{
    internal partial class SerialCom : IDisposable
    {
        private static Stopwatch sw = new Stopwatch();
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
                        instance = new SerialCom();

                    if(instance.XON && MessageQueue.Count > 0) 
                    {
                        //if(MessageQueue.Peek().Raw.Length > instance.remoteBufferLeft)
                        //{
                        //    instance.XON = false;
                        //    continue;
                        //}

                        var item = MessageQueue.Dequeue();
                        if(!MessageQueue.Any(x => x.Type == item.Type))
                        {
                            //instance.remoteBufferLeft -= item.Raw.Length;
                            instance.Write(item.Raw);
                        }
                        else {
                            Console.WriteLine($"{item.Type.ToString()} Message Dropped");
                        }
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