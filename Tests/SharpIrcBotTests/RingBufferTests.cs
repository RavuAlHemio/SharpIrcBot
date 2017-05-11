using System;
using System.Collections.Generic;
using SharpIrcBot.Collections;
using Xunit;

namespace SharpIrcBot.Tests.SharpIrcBotTests
{
    public class RingBufferTests
    {
        private void AssertRBEqual(RingBuffer<int> rb, params int[] expectedElements)
        {
            Assert.Equal((IEnumerable<int>)expectedElements, rb);
        }

        // pi ~ 3.14159265358979323846264338327950288
        // remove repeating digits: 3 1 4 5 9 2 6 8 7 0

        [Fact]
        public void TestList4()
        {
            var rb = new RingBuffer<int>(4);

            AssertRBEqual(rb, new int[0]);

            rb.Add(3);
            AssertRBEqual(rb, 3);

            rb.Add(1);
            AssertRBEqual(rb, 3, 1);

            rb.Add(4);
            AssertRBEqual(rb, 3, 1, 4);

            rb.Add(5);
            AssertRBEqual(rb, 3, 1, 4, 5);

            rb.Add(9);
            AssertRBEqual(rb, 1, 4, 5, 9);

            rb.Add(2);
            AssertRBEqual(rb, 4, 5, 9, 2);

            rb.Add(6);
            AssertRBEqual(rb, 5, 9, 2, 6);

            rb.Add(8);
            AssertRBEqual(rb, 9, 2, 6, 8);

            rb.Add(7);
            AssertRBEqual(rb, 2, 6, 8, 7);

            rb.Add(0);
            AssertRBEqual(rb, 6, 8, 7, 0);
        }

        [Fact]
        public void TestList3()
        {
            var rb = new RingBuffer<int>(3);

            AssertRBEqual(rb, new int[0]);

            rb.Add(3);
            AssertRBEqual(rb, 3);

            rb.Add(1);
            AssertRBEqual(rb, 3, 1);

            rb.Add(4);
            AssertRBEqual(rb, 3, 1, 4);

            rb.Add(5);
            AssertRBEqual(rb, 1, 4, 5);

            rb.Add(9);
            AssertRBEqual(rb, 4, 5, 9);

            rb.Add(2);
            AssertRBEqual(rb, 5, 9, 2);

            rb.Add(6);
            AssertRBEqual(rb, 9, 2, 6);

            rb.Add(8);
            AssertRBEqual(rb, 2, 6, 8);

            rb.Add(7);
            AssertRBEqual(rb, 6, 8, 7);

            rb.Add(0);
            AssertRBEqual(rb, 8, 7, 0);
        }

        [Fact]
        public void TestList1()
        {
            var rb = new RingBuffer<int>(1);

            AssertRBEqual(rb, new int[0]);

            rb.Add(3);
            AssertRBEqual(rb, 3);

            rb.Add(1);
            AssertRBEqual(rb, 1);

            rb.Add(4);
            AssertRBEqual(rb, 4);

            rb.Add(5);
            AssertRBEqual(rb, 5);

            rb.Add(9);
            AssertRBEqual(rb, 9);

            rb.Add(2);
            AssertRBEqual(rb, 2);

            rb.Add(6);
            AssertRBEqual(rb, 6);

            rb.Add(8);
            AssertRBEqual(rb, 8);

            rb.Add(7);
            AssertRBEqual(rb, 7);

            rb.Add(0);
            AssertRBEqual(rb, 0);
        }
    }
}
