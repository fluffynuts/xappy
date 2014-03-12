using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using NUnit.Framework;

namespace GIPC.Tests
{
    [TestFixture]
    class TestGIPCServer
    {
        protected List<GIPCServer> servers = new List<GIPCServer>();

        [TestFixtureSetUp]
        public void __init__()
        {
        }

        [TestFixtureTearDown]
        public void __deinit__()
        {
            //foreach (var s in this.servers)
            //    s.Dispose();
            //this.servers.Clear();
        }

        protected GIPCServer GenGIPCServer()
        {
            var id = RandomValueGen.GetRandomString();
            var server = new GIPCServer(GIPCServer.Protocols.NamedPipe, id);
            this.servers.Add(server);   // ensure this server is stopped properly
            return server;
        }
        [Test]
        public void ServerDelegateIsCalled()
        {
            //---------------Set up test pack-------------------
            using (var server = this.GenGIPCServer())
            {
                string receivedMessage = null;
                var expected = new string[] { RandomValueGen.GetRandomString() };
                server.OnMessageReceived += (msg) =>
                {
                    receivedMessage = msg;
                    return msg;
                };
                server.Start();

                //---------------Assert Precondition----------------
                Assert.IsNull(receivedMessage, "Received message should be null before the test");
                Assert.IsTrue(server.Running, "GIPC server isn't running");

                //---------------Execute Test ----------------------
                using (var client = new GIPCClient(GIPCBase.Protocols.NamedPipe, server.Identifier))
                {
                    var ret = client.SendMessage(expected[0]);

                    //---------------Test Result -----------------------
                    Assert.AreEqual(expected, ret, "ECHO lambda doesn't return original string");
                    Assert.AreEqual(expected[0], receivedMessage, "ECHO lambda not called");
                }
            }
        }

        [Test]
        public void WithExistingServer_CanChangeServerID()
        {
            //---------------Set up test pack-------------------
            using (var svr = this.GenGIPCServer())
            {
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
                    string[] msg2 = null;
                    using (var client2 = new GIPCClient(GIPCBase.Protocols.NamedPipe, id2))
                    {
                        Assert.DoesNotThrow(() =>
                            {
                                msg2 = client2.SendMessage("test2");
                            });
                        //---------------Test Result -----------------------
                        Assert.AreEqual(msg1.Length, 1, "first message return queue is of unexpected length");
                        Assert.AreEqual(msg2.Length, 1, "second message return queue is of unexpected length");
                        Assert.AreEqual(msg1[0], "test1", "First message is mangled");
                        Assert.AreEqual(msg2[0], "test2", "second message is mangled");
                    }
                }
            }
        }

        class GIPCServerSpy : GIPCServer
        {
            public Protocols OverrideProtocol { get { return this._protocol; } set { this._protocol = value; } }
            public GIPCServerSpy(Protocols protocol, string id)
                : base(protocol, id)
            {
            }
        }

        [Test]
        public void WithExistingServer_CanChangeServerProtocol()
        {
            //---------------Set up test pack-------------------
            var id = RandomValueGen.GetRandomString();
            using (var svr = new GIPCServerSpy(GIPCServer.Protocols.NamedPipe, id))
            {
                this.servers.Add(svr);   // ensure this server is stopped properly

                var id1 = svr.Identifier;
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
                    svr.OverrideProtocol = GIPCBase.Protocols.None;
                    svr.Protocol = GIPCBase.Protocols.NamedPipe;
                    string[] msg2 = null;
                    using (var client2 = new GIPCClient(GIPCBase.Protocols.NamedPipe, id1))
                    {
                        Assert.DoesNotThrow(() =>
                            {
                                msg2 = client2.SendMessage("test2");
                            });
                        //---------------Test Result -----------------------
                        Assert.AreEqual(msg1.Length, 1, "first message return queue is of unexpected length");
                        Assert.AreEqual(msg2.Length, 1, "second message return queue is of unexpected length");
                        Assert.AreEqual(msg1[0], "test1", "First message is mangled");
                        Assert.AreEqual(msg2[0], "test2", "second message is mangled");
                    }
                }
            }
        }

        [Test]
        public void Start_ThrowsProtocolNotImplementedExceptionWithUnhandledProtocol()
        {
            //---------------Set up test pack-------------------
            using (var svr = this.GenGIPCServer())
            {
                svr.Protocol = GIPCBase.Protocols.None;

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                Assert.Throws(typeof(ProtocolNotImplementedException), () =>
                                                                           {
                                                                               svr.Start();
                                                                           });
                //---------------Test Result -----------------------
            }
        }

        [Test]  // trying to debug why tests fail when run in tandem. Conclusion: WCF or WCF's usage of named pipes has issues.
        public void Start_CanStartMultipleServers_EachCanReceiveAMessage()
        {
            //---------------Set up test pack-------------------
            var id1 = RandomValueGen.GetRandomString();
            var id2 = RandomValueGen.GetRandomString();
            Assert.AreNotEqual(id1, id2);
            using (var svr1 = new GIPCServer(GIPCBase.Protocols.NamedPipe, id1))
            {
                using (var svr2 = new GIPCServer(GIPCBase.Protocols.NamedPipe, id2))
                {
                    using (var c1 = new GIPCClient(GIPCBase.Protocols.NamedPipe, id1))
                    {
                        using (var c2 = new GIPCClient(GIPCBase.Protocols.NamedPipe, id2))
                        {
                            //---------------Assert Precondition----------------
                            var called1 = false;
                            var called2 = false;
                            svr1.OnMessageReceived += (m) =>
                                {
                                    called1 = true;
                                    return "OK";
                                };
                            svr2.OnMessageReceived += (m) =>
                                {
                                    called2 = true;
                                    return "OK";
                                };
                            //---------------Execute Test ----------------------
                            svr1.Start();
                            svr2.Start();
                            c1.SendMessage(RandomValueGen.GetRandomString());
                            c2.SendMessage(RandomValueGen.GetRandomString());


                            //---------------Test Result -----------------------
                            Assert.IsTrue(called1);
                            Assert.IsTrue(called2);
                        }
                    }
                }
            }
        }

        [Test]
        public void ParametersChanged_EventRaisedWhenIdentifierChanged()
        {
            //---------------Set up test pack-------------------
            var svr = new GIPCServer(GIPCServer.Protocols.NamedPipe, RandomValueGen.GetRandomString());
            string changedParameter = null;
            svr.ParameterChanged += (s, e) =>
            {
                changedParameter = e.ChangedParameter;
            };
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            svr.Identifier = RandomValueGen.GetRandomString();

            //---------------Test Result -----------------------
            Assert.IsNotNull(changedParameter);
            Assert.AreEqual("Identifier", changedParameter);
        }
    }
}
