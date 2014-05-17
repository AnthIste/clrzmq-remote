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
        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var client = context.Socket(SocketType.REP))
            using (var discovery = context.Socket(SocketType.REQ))
            {
                client.Identity = Encoding.UTF8.GetBytes("Foobar2000");
                client.Connect("tcp://localhost:5555");

                discovery.Connect("tcp://localhost:5556");
                discovery.SendMore("svc:add", Encoding.UTF8);
                discovery.Send("Foobar2000", Encoding.UTF8);
                discovery.RecvAll();

                var clientPollItem = client.CreatePollItem(IOMultiPlex.POLLIN);
                var pollItems = new[] { clientPollItem };

                clientPollItem.PollInHandler += PollClient;

                while (true)
                {
                    context.Poll(pollItems);
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
