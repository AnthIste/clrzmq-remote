using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZMQ;

namespace NetMQ
{
    public static class SocketExtensions
    {
        public static void SendMessage(this Socket socket, Message message)
        {
            if (!message.Frames.Any())
            {
                throw new InvalidOperationException("Cannot send message with 0 frames");
            }

            if (message.Header.Any())
            {
                foreach (var frame in message.Header)
                {
                    socket.SendMore(frame);
                }
            }

            foreach (var frame in message.Frames.Take(message.Frames.Count - 1).ToArray())
            {
                socket.SendMore(frame);
            }

            socket.Send(message.Frames.Last());
        }

        public static Message RecvMessage(this Socket socket)
        {
            return Message.FromFrames(socket.RecvAll());
        }
    }
}
