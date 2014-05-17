using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public sealed class Message
    {
        public IList<byte[]> Header { get; set; }

        public IList<byte[]> Frames { get; set; }

        private Message()
        {
        }

        public void Wrap(byte[] frame)
        {
            Header.Insert(0, new byte[] { });
            Header.Insert(0, frame);
        }

        public void Wrap(IList<byte[]> frames)
        {
            var frameStack = new Stack<byte[]>(frames);

            while (frameStack.Any())
            {
                Header.Insert(0, frameStack.Pop());
            }
        }

        public List<byte[]> Unwrap()
        {
            var frames = new List<byte[]>();

            while (Header.Any())
            {
                var frame = Header[0];

                frames.Add(frame);
                Header.RemoveAt(0);

                if (frame.Length == 0)
                {
                    break;
                }
            }

            return frames;
        }

        public static Message FromFrame(byte[] frame)
        {
            return new Message
            {
                Header = new List<byte[]>(),
                Frames = new List<byte[]> { frame }
            };
        }

        public static Message FromFrames(IEnumerable<byte[]> frames)
        {
            var frameStack = new Stack<byte[]>(frames);
            var message = new Message
            {
                Header = new List<byte[]>(),
                Frames = new List<byte[]>()
            };

            while (frameStack.Any() && (frameStack.Peek().Length > 0 || !message.Frames.Any()))
            {
                message.Frames.Insert(0, frameStack.Pop());
            }

            //if (frameStack.Any() && !message.Frames.Any())
            //{
            //    message.Frames.Insert(0, frameStack.Pop());
            //}

            while (frameStack.Any())
            {
                message.Header.Insert(0, frameStack.Pop());
            }

            return message;
        }
    }
}
