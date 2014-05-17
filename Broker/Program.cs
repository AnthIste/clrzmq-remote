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

                servicesPollItem.PollInHandler += (socket, revents) => SwitchBackToFront(socket, frontend);
                clientsPollItem.PollInHandler += (socket, revents) => SwitchFrontToBack(socket, backend);

                while (true)
                {
                    context.Poll(pollItems);
                }
            }
        }

        private static void SwitchFrontToBack(Socket from, Socket to)
        {
            var request = Message.FromFrames(from.RecvAll());

            // TODO: replace with inspection code
            var requestingClient = request.Unwrap()[0];
            var targetService = request.Unwrap()[0];
            request.Wrap(targetService);
            request.Wrap(requestingClient);

            request.Wrap(targetService); // Target ROUTER id

            Console.WriteLine("Switching {0} -> {1}",
                Encoding.UTF8.GetString(requestingClient),
                Encoding.UTF8.GetString(targetService));

            to.SendMessage(request);
        }

        private static void SwitchBackToFront(Socket from, Socket to)
        {
            var request = Message.FromFrames(from.RecvAll());

            // TODO: replace with inspection code
            var targetService = request.Unwrap()[0];
            var requestingClient = request.Unwrap()[0];
            request.Wrap(requestingClient);

            Console.WriteLine("Switching {0} -> {1}",
                Encoding.UTF8.GetString(targetService),
                Encoding.UTF8.GetString(requestingClient));

            to.SendMessage(request);
        }
    }
}
