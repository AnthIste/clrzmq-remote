using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using ZMQ;

namespace Service
{
    class Program
    {
        private const string Identity = "Winamp";

        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var client = context.Socket(SocketType.ROUTER))
            {
                client.Identity = Encoding.UTF8.GetBytes(Identity);
                client.Connect("tcp://localhost:5555");

                var clientPollItem = client.CreatePollItem(IOMultiPlex.POLLIN);
                var pollItems = new[] { clientPollItem };

                clientPollItem.PollInHandler += PollClient;

                var worker = new Thread(() => HeartbeatThread(context));
                worker.Start();

                while (true)
                {
                    context.Poll(pollItems);
                }
            }
        }

        private static void HeartbeatThread(Context context)
        {
            using (var heartbeat = context.Socket(SocketType.PUB))
            {
                heartbeat.Connect("tcp://localhost:5554");

                while (true)
                {
                    heartbeat.Send(Encoding.UTF8.GetBytes(Identity));

                    Thread.Sleep(500);
                }
            }
        }

        private static void PollClient(Socket socket, IOMultiPlex revents)
        {
            var request = socket.RecvMessage();

            Console.WriteLine("Received client request: {0}", Encoding.UTF8.GetString(request.Frames[0]));

            var response = Message.FromFrame(Encoding.UTF8.GetBytes("ACK"));
            response.Wrap(request.Header);

            socket.SendMessage(response);
        }
    }
}
