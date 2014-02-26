using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xappy.Tests
{
    [TestFixture]
    [Ignore("This isn't working yet and it's just an idea for now")]
    public class TestInterleavedStreamLineReader
    {
        [Test]
        public void Construct_ShouldThrowIfGivenNoStreams()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var ex = Assert.Throws<NoStreamsProvidedException>(() => new InterleavedStreamLineReader());

            //---------------Test Result -----------------------
        }

        [Test]
        public void TwoStreams_SimpleInterleave()
        {
            //---------------Set up test pack-------------------
            var s1 = new MemoryStream();
            var s2 = new MemoryStream();
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var reader = new InterleavedStreamLineReader(s1, s2);

            var saidHello = false;
            var saidThere = false;

            var t1 = Task.Run(() =>
                {
                    var writer = new StreamWriter(s1);
                    writer.WriteLine("hello");
                    writer.Flush();
                    saidHello = true;
                    while (!saidThere)
                        Thread.Sleep(100);
                    writer.WriteLine("world");
                    writer.Flush();
                    //s1.Close();
                });
            var t2 = Task.Run(() =>
                {
                    var writer = new StreamWriter(s2);
                    while (!saidHello)
                        Thread.Sleep(100);
                    writer.WriteLine("there");
                    writer.Flush();
                    saidThere = true;
                    //s2.Close();
                });
            t1.Wait();
            t2.Wait();
            Thread.Sleep(50000);
            var result = reader.GetInterleavedOutput();
            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("hello", result[0]);
            Assert.AreEqual("there", result[1]);
            Assert.AreEqual("world", result[2]);
        }
    }
}
