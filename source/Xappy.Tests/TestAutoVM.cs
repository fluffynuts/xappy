using System.Management;
using NUnit.Framework;

namespace Xappy.Tests
{
    [TestFixture]
    public class TestAutoVM
    {
        [Test]
        [Ignore("Looking for a way to automate the VM a bit more")]
        public void Discovery()
        {
            //---------------Set up test pack-------------------
            var VMName = "Emulator WVGA 512MB.davyd";
            var searcher = new ManagementObjectSearcher("SELECT * FROM Msvm_ComputerSystem WHERE Caption='Virtual Machine' and ElementName like '" + VMName + "'");
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------

            //---------------Test Result -----------------------
            Assert.Fail("Test Not Yet Implemented");
        }
    }
}
