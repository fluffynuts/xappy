using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Habanero.Testability;
using NSubstitute;
using NUnit.Framework;

namespace GIPC.Tests
{
    [TestFixture]
    public class TestGIPCBase
    {
        [Test]
        public void GenerateEndpointAddress_GeneratesCorrectNamedPipeAddress()
        {
            //---------------Set up test pack-------------------
            var id = RandomValueGen.GetRandomString();
            var b = new GIPCBase() { Protocol = GIPCBase.Protocols.NamedPipe, Identifier = id };
            var expected = GIPCBase.NamedPipeProtocol + b.HostName + "/" + GIPCBase.AddressBaseName + "/" + id;

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = b.GenerateEndPointAddress();

            //---------------Test Result -----------------------
            Assert.AreEqual(expected, result, "Generic named pipe generator generates unexpected pipe name");
        }

        [Test]
        public void GenerateEndpointAddress_GeneratesCorrectNetTcpAddress()
        {
            //---------------Set up test pack-------------------
            var id = RandomValueGen.GetRandomString();
            var sut = new GIPCBase() { Protocol = GIPCBase.Protocols.NetTcp, Identifier = id };
            var expected = GIPCBase.NetTcpProtocol + sut.HostName + ":" + sut.Port + "/" + GIPCBase.AddressBaseName + "/" + id;
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = sut.GenerateEndPointAddress();

            //---------------Test Result -----------------------
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GenerateEndpointAddress_RemovesInvalidURICharactersAndSetsIdentifierSanitisedToTrue()
        {
            //---------------Set up test pack-------------------
            var id = "{[]::abc";
            var b = new GIPCBase() { Protocol = GIPCBase.Protocols.NamedPipe };

            //---------------Assert Precondition----------------
            Assert.IsFalse(b.IdentifierSanitised, "IdentifierSanitised should be false when no identifier set");
            Assert.IsNull(b.Identifier, "identifier should not be set");
            //---------------Execute Test ----------------------
            b.Identifier = id;
            //---------------Test Result -----------------------
            Assert.IsNotNull(b.Identifier, "Identifier should have been set");
            Assert.AreNotEqual(id, b.Identifier, "Identifier should have been modified");
            Assert.IsTrue(b.IdentifierSanitised, "identifier should have been marked as sanitised");
        }
    }
}
