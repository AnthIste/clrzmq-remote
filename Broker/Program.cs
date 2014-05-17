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
        private static readonly HashSet<string> ActiveServices = new HashSet<string>();

        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var backend = context.Socket(SocketType.ROUTER))
            using (var frontend = context.Socket(SocketType.ROUTER))
            using (var discovery = context.Socket(SocketType.ROUTER))
            {
                backend.Bind("tcp://*:5555");
                frontend.Bind("tcp://*:6666");
                discovery.Bind("tcp://*:5556");

                var servicesPollItem = backend.CreatePollItem(IOMultiPlex.POLLIN);
                var clientsPollItem = frontend.CreatePollItem(IOMultiPlex.POLLIN);
                var discoveryPollItem = discovery.CreatePollItem(IOMultiPlex.POLLIN);

                var pollItems = new[] { servicesPollItem, clientsPollItem, discoveryPollItem };

                servicesPollItem.PollInHandler += (socket, revents) => SwitchBackToFront(socket, frontend);
                clientsPollItem.PollInHandler += (socket, revents) => SwitchFrontToBack(socket, backend);
                discoveryPollItem.PollInHandler += PollDiscoverySocket;

                while (true)
                {
                    context.Poll(pollItems);
                }
            }
        }

        private static void SwitchFrontToBack(Socket frontend, Socket backend)
        {
            var request = frontend.RecvMessage();

            var requestingClient = request.Unwrap()[0]; // Incoming ROUTER id
            var targetService = request.Unwrap()[0];    // Outgoing ROUTER id

            request.Wrap(requestingClient);             // Preserve existing state
            request.Wrap(targetService);                // Explicitly attach outgoing ROUTER id

            Console.WriteLine("Switching {0} -> {1}",
                Encoding.UTF8.GetString(requestingClient),
                Encoding.UTF8.GetString(targetService));

            backend.SendMessage(request);
        }

        private static void SwitchBackToFront(Socket backend, Socket frontend)
        {
            var response = backend.RecvMessage();

            // Remove previously attached ROUTER id
            var targetService = response.Unwrap()[0];

            // TODO: replace with inspection code
            var requestingClient = response.Unwrap()[0];
            response.Wrap(requestingClient);

            Console.WriteLine("Switching {0} -> {1}",
                Encoding.UTF8.GetString(targetService),
                Encoding.UTF8.GetString(requestingClient));

            frontend.SendMessage(response);
        }

        private static void PollDiscoverySocket(Socket socket, IOMultiPlex revents)
        {
            var request = socket.RecvMessage();

            Message response;
            switch (Encoding.UTF8.GetString(request.Frames[0]))
            {
                case "svc:add":
                    ActiveServices.Add(Encoding.UTF8.GetString(request.Frames[1]));
                    SvcGenericResponse(out response);
                    break;

                case "svc:rm":
                    ActiveServices.Remove(Encoding.UTF8.GetString(request.Frames[1]));
                    SvcGenericResponse(out response);
                    break;

                case "svc:getactive":
                    SvcGetActiveResponse(out response);
                    break;

                default:
                    SvcGenericResponse(out response);
                    break;
            }

            response.Wrap(request.Header);

            socket.SendMessage(response);
        }

        private static void SvcGenericResponse(out Message message)
        {
            message = Message.FromFrame(new byte[] { });
        }

        private static void SvcGetActiveResponse(out Message message)
        {
            var frames = ActiveServices.Select(Encoding.UTF8.GetBytes).ToList();
            frames.Add(new byte[] { });

            message = Message.FromFrames(frames);
        }
    }
}
