﻿using System;
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
        private static readonly Dictionary<string, long> ServiceActivity = new Dictionary<string, long>();

        private const long ActivityThresholdSeconds = 5;

        static void Main(string[] args)
        {
            using (var context = new Context())
            using (var backend = context.Socket(SocketType.ROUTER))
            using (var frontend = context.Socket(SocketType.ROUTER))
            using (var heartbeat = context.Socket(SocketType.SUB))
            using (var discovery = context.Socket(SocketType.ROUTER))
            {
                backend.Bind("tcp://*:5555");
                frontend.Bind("tcp://*:6666");
                heartbeat.Bind("tcp://*:5554");
                discovery.Bind("tcp://*:5556");

                heartbeat.Subscribe("", Encoding.UTF8);

                var servicesPollItem = backend.CreatePollItem(IOMultiPlex.POLLIN);
                var clientsPollItem = frontend.CreatePollItem(IOMultiPlex.POLLIN);
                var heartbeatPollItem = heartbeat.CreatePollItem(IOMultiPlex.POLLIN);
                var discoveryPollItem = discovery.CreatePollItem(IOMultiPlex.POLLIN);

                var pollItems = new[] { servicesPollItem, clientsPollItem, heartbeatPollItem, discoveryPollItem };

                servicesPollItem.PollInHandler += (socket, revents) => SwitchBackToFront(socket, frontend);
                clientsPollItem.PollInHandler += (socket, revents) => SwitchFrontToBack(socket, backend);
                heartbeatPollItem.PollInHandler += PollHeartbeatSocket;
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
            var respondingService = response.Unwrap()[0];

            // TODO: replace with inspection code
            var targetClient = response.Unwrap()[0];
            response.Wrap(targetClient);

            Console.WriteLine("Switching {0} -> {1}",
                Encoding.UTF8.GetString(respondingService),
                Encoding.UTF8.GetString(targetClient));

            frontend.SendMessage(response);
        }

        private static void PollHeartbeatSocket(Socket socket, IOMultiPlex revents)
        {
            var request = socket.RecvMessage();
            var serviceName = Encoding.UTF8.GetString(request.Frames[0]);

            ServiceActivity[serviceName] = DateTime.Now.Ticks;
        }

        private static void PollDiscoverySocket(Socket socket, IOMultiPlex revents)
        {
            var request = socket.RecvMessage();
            var command = Encoding.UTF8.GetString(request.Frames[0]);

            Message response;
            switch (command)
            {
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
            var activeServices = ServiceActivity
                .Where(kvp => UpToDate(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            var frames = activeServices.Select(Encoding.UTF8.GetBytes).ToList();
            frames.Add(new byte[] { });

            message = Message.FromFrames(frames);
        }

        private static bool UpToDate(long lastUpdateTick)
        {
            var currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            var updateTime = lastUpdateTick / TimeSpan.TicksPerSecond;

            return (currentTime - updateTime) <= ActivityThresholdSeconds;
        }
    }
}
