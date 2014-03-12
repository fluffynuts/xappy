using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GIPC.Tests
{
    [TestFixture]
    class TestGIPClient : TestGIPCServer
    {
        [Test]
        public void NamedPipeClientConnectsToServerWithoutException()
        {
            //---------------Set up test pack-------------------
            using (var server = this.GenGIPCServer())
            {
                Assert.DoesNotThrow(() =>
                    {
                        server.Start();
                    });

                using (var client = new GIPCClient(GIPCBase.Protocols.NamedPipe, server.Identifier))
                {

                    //---------------Assert Precondition----------------

                    //---------------Execute Test ----------------------
                    Assert.DoesNotThrow(() =>
                        {
                            client.SendMessage("test");
                        });
                }
                //---------------Test Result -----------------------
            }
        }

        [Test]
        public void NetTcpClientConnectsToServerWithoutException()
        {
            //---------------Set up test pack-------------------
            var id = RandomValueGen.GetRandomString();
            var port = RandomValueGen.GetRandomInt(2000, 10000);
            using (var svr = new GIPCServer(GIPCBase.Protocols.NetTcp, id))
            {
                svr.Port = port;
                Assert.DoesNotThrow(svr.Start);
                using (var client = new GIPCClient(GIPCBase.Protocols.NetTcp, id))
                {
                    client.Port = port;
                    //---------------Assert Precondition----------------

                    //---------------Execute Test ----------------------
                    Assert.DoesNotThrow(() => client.SendMessage("test"));

                    //---------------Test Result -----------------------
                }
            }
        }

        class GIPCClient_ExposedProtocolWithNoHandler : GIPCClient
        {
            public Protocols OverrideProtocol { get { return this._protocol; } set { this._protocol = value; } }
        }

        [Test]
        public void WithExistingClient_ClientCanBeReused()
        {
            //---------------Set up test pack-------------------
            var svr1 = this.GenGIPCServer();
            svr1.Start();
            var svr2 = this.GenGIPCServer();
            svr2.Start();
            var svr3 = this.GenGIPCServer();
            svr3.Start();

            var expected = new Dictionary<string, string>();
            expected[svr1.Identifier] = RandomValueGen.GetRandomString();
            expected[svr2.Identifier] = RandomValueGen.GetRandomString();
            expected[svr3.Identifier] = RandomValueGen.GetRandomString();

            var actual = new Dictionary<string, string>();

            svr1.OnMessageReceived += (msg) =>
                {
                    actual[svr1.Identifier] = msg;
                    return msg;
                };
            svr2.OnMessageReceived += (msg) =>
                {
                    actual[svr2.Identifier] = msg;
                    return msg;
                };

            //---------------Assert Precondition----------------
            Assert.IsFalse(actual.Keys.Contains(svr1.Identifier), "Actual results already contains key for svr1");
            Assert.IsFalse(actual.Keys.Contains(svr2.Identifier), "Actual results already contains key for svr2");

            //---------------Execute Test ----------------------
            using (var client = new GIPCClient_ExposedProtocolWithNoHandler())
            {
                client.SendMessage(expected[svr1.Identifier], GIPCBase.Protocols.NamedPipe, svr1.Identifier);
                client.SendMessage(expected[svr2.Identifier], GIPCBase.Protocols.NamedPipe, svr2.Identifier);
                client.OverrideProtocol = GIPCBase.Protocols.None;  // force a reconnect due to apparent protocol change
                client.SendMessage(expected[svr3.Identifier], GIPCBase.Protocols.NamedPipe, svr3.Identifier);

                //---------------Test Result -----------------------
                Assert.IsTrue(actual.Keys.Contains(svr1.Identifier), "Actual results don't contain svr1 key");
                Assert.IsTrue(actual.Keys.Contains(svr2.Identifier), "Actual results don't contain svr2 key");
                Assert.AreEqual(actual[svr1.Identifier], expected[svr1.Identifier], "Actual results != expected results");
                Assert.AreEqual(actual[svr2.Identifier], expected[svr2.Identifier], "Actual results != expected results");
            }
        }

        [Test]
        public void WithExistingClient_CanChangeClientID()
        {
            //---------------Set up test pack-------------------
            var svr = this.GenGIPCServer();
            var id1 = svr.Identifier;
            var id2 = RandomValueGen.GetRandomString();
            svr.Start();
            var calls = 0;
            svr.OnMessageReceived += (msg) =>
            {
                calls++;
                return msg;
            };
            //---------------Assert Precondition----------------
            Assert.AreEqual(0, calls);
            //---------------Execute Test ----------------------
            using (var client = new GIPCClient(GIPCBase.Protocols.NamedPipe, id1))
            {

                var msg1 = client.SendMessage("test1");
                svr.Identifier = id2;
                client.Identifier = id2;
                string[] msg2 = null;
                Assert.DoesNotThrow(() =>
                    {
                        msg2 = client.SendMessage("test2");
                    });
                Assert.AreEqual(msg1.Length, 1, "first message return queue is of unexpected length");
                Assert.AreEqual(msg2.Length, 1, "second message return queue is of unexpected length");
                Assert.AreEqual(msg1[0], "test1", "First message is mangled");
                Assert.AreEqual(msg2[0], "test2", "second message is mangled");
            }
        }

        [Test]
        public void ProtocolNone_ThrowsProtocolNotImplementedException()
        {
            //---------------Set up test pack-------------------
            Assert.Throws(typeof(ProtocolNotImplementedException), () =>
                                                                       {
                                                                           var client =
                                                                               new GIPCClient(GIPCBase.Protocols.None,
                                                                                              RandomValueGen.
                                                                                                  GetRandomString());
                                                                           client.Connect();
                                                                       }, "Should throw ProtocolNotImplementedException");
        }

        class GIPCClientSpy : GIPCClient
        {
            public int ConnectCallCount { get; protected set; }
            public override bool Connected { get { return true; } }
            public GIPCClientSpy(GIPCBase.Protocols protocol, string identifier)
                : base (protocol, identifier)
            {
                this.ConnectCallCount = 0;
            }

            public override void Connect(Protocols? protocol = null, string identifier = null)
            {
                this.ConnectCallCount++;
            }
        }

        [Test]
        public void ClientProtocolSet_WhenChangingProtocolsReconnects()
        {
            //---------------Set up test pack-------------------
            var id = RandomValueGen.GetRandomString();
            var client = new GIPCClientSpy(GIPCBase.Protocols.None, id);
            var constructorCalledCount = client.ConnectCallCount;

            //---------------Assert Precondition----------------
            

            //---------------Execute Test ----------------------
            client.Protocol = GIPCBase.Protocols.NamedPipe;
            //---------------Test Result -----------------------
            Assert.That(client.ConnectCallCount, Is.GreaterThan(constructorCalledCount), "Connect not called when protocol changed");
        }
    }
}
