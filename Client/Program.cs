using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using ZMQ;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var service = context.Socket(SocketType.REQ))
            {
                service.Identity = Encoding.UTF8.GetBytes("client2");
                service.Connect("tcp://localhost:6666");

                while (true)
                {
                    Console.WriteLine("Sending request");

                    var request = Message.FromFrame(Encoding.UTF8.GetBytes("WARTRRGH!"));
                    request.Wrap(Encoding.UTF8.GetBytes("Winamp"));
                    Debug.Assert(request.Header.Count == 2);

                    service.SendMessage(request);

                    var response = service.RecvMessage();
                    Debug.Assert(response.Header.Count == 2);

                    Console.WriteLine("Response: {0}", Encoding.UTF8.GetString(response.Frames[0]));

                    Thread.Sleep(500);
                }
            }
        }
    }
}
