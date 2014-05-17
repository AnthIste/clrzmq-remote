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
            using (var services = context.Socket(SocketType.REQ))
            using (var discovery = context.Socket(SocketType.REQ))
            {
                services.Identity = Encoding.UTF8.GetBytes("client1");
                services.Connect("tcp://localhost:6666");
                discovery.Connect("tcp://localhost:5556");

                while (true)
                {
                    //{
                    //    Console.WriteLine("Sending request...");

                    //    var request = Message.FromFrame(Encoding.UTF8.GetBytes("WARTRRGH!"));

                    //    request.Wrap(Encoding.UTF8.GetBytes("Misc header 1"));
                    //    request.Wrap(Encoding.UTF8.GetBytes("Misc header 2"));
                    //    request.Wrap(Encoding.UTF8.GetBytes("Winamp"));
                    //    Debug.Assert(request.Header.Count == 6);

                    //    services.SendMessage(request);

                    //    var response = services.RecvMessage();
                    //    Debug.Assert(response.Header.Count == 4);

                    //    Console.WriteLine("Response: {0}", Encoding.UTF8.GetString(response.Frames[0]));
                    //}

                    {
                        Console.WriteLine("Checking active services...");

                        var request = Message.FromFrame(Encoding.UTF8.GetBytes("svc:getactive"));

                        discovery.SendMessage(request);

                        var response = discovery.RecvMessage();

                        if (!response.Frames.Any())
                        {
                            Console.WriteLine("0 active services");
                        }
                        else
                        {
                            var serviceNames = response.Frames
                                .Where(x => x.Length > 0)
                                .Select(Encoding.UTF8.GetString)
                                .ToArray();

                            Console.WriteLine("{0} services found:", serviceNames.Length);

                            foreach (var serviceName in serviceNames)
                            {
                                Console.WriteLine(" - {0}", serviceName);
                            }
                        }
                    }

                    Thread.Sleep(1500);
                }
            }
        }
    }
}
