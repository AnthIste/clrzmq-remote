using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using ZMQ;

namespace Broker
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var backend = context.Socket(SocketType.ROUTER))
            using (var frontend = context.Socket(SocketType.ROUTER))
            {
                backend.Bind("tcp://*:5555");
                frontend.Bind("tcp://*:6666");

                var servicesPollItem = backend.CreatePollItem(IOMultiPlex.POLLIN);
                var clientsPollItem = frontend.CreatePollItem(IOMultiPlex.POLLIN);

                var pollItems = new[] { servicesPollItem, clientsPollItem };

                servicesPollItem.PollInHandler += (socket, revents) => Switch(socket, frontend);
                clientsPollItem.PollInHandler += (socket, revents) => Switch(socket, backend);

                while (true)
                {
                    context.Poll(pollItems);
                }
            }
        }

        private static void Switch(Socket from, Socket to)
        {
            var request = Message.FromFrames(from.RecvAll());

            var requestingClient = request.Unwrap()[0]; // Request ROUTER id
            var targetService = request.Unwrap()[0];    // Target ROUTER id

            request.Wrap(requestingClient);             // Request ROUTER id
            request.Wrap(targetService);                // Target ROUTER id

            Console.WriteLine("Switching {0}/{1}",
                Encoding.UTF8.GetString(requestingClient),
                Encoding.UTF8.GetString(targetService));

            to.SendMessage(request);
        }
    }
}
