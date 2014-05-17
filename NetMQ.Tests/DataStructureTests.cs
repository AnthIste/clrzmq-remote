using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NetMQ.Tests
{
    [TestFixture]
    public class DataStructureTests
    {
        [Test]
        public void Stack_Pop_PopFromBack()
        {
            var list = new List<int> { 1, 2, 3 };
            var stack = new Stack<int>(list);

            Assert.AreEqual(3, stack.First());
            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.First());
        }

        [Test]
        public void Queue_Dequeue_DequeueFromFront()
        {
            var list = new List<int> { 1, 2, 3 };
            var queue = new Queue<int>(list);

            Assert.AreEqual(1, queue.First());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.First());
        }

        [Test]
        public void Queue_Enqueue_EnqueueToBack()
        {
            var list = new List<int> { 1, 2, 3 };
            var queue = new Queue<int>(list);

            Assert.AreEqual(3, queue.Last());
            queue.Enqueue(4);
            Assert.AreEqual(4, queue.Last());
        }

        [Test]
        public void Dictionary_ArrayAsKey_ItemsNotFound()
        {
            var a1 = new byte[] { 1, 2, 3 };
            var a2 = new byte[] { 1, 2, 3 };
            var d = new Dictionary<byte[], string>();

            d[a1] = "foo";

            Assert.Throws<KeyNotFoundException>(() =>
            {
                var x = d[a2];
            });
        }

        [Test]
        public void Dictionary_HashCodeAsKey_ItemsNotFound()
        {
            var a1 = new byte[] { 1, 2, 3 };
            var a2 = new byte[] { 1, 2, 3 };
            var d = new Dictionary<int, string>();

            d[a1.GetHashCode()] = "foo";

            Assert.Throws<KeyNotFoundException>(() =>
            {
                var x = d[a2.GetHashCode()];
            });
        }
    }
}
