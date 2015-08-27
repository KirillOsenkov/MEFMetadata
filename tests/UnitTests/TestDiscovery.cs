using System.IO;
using System.Reflection;
using Microsoft.Composition.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TestDiscovery
    {
        [TestMethod]
        public void TestDiscovery1()
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var resolver = new FolderBasedAssemblyResolver(directory);
            var discovery = new Discovery(resolver);
            var info = discovery.GetAssemblyCatalogInfoFromFile("TestAssemblyA.dll");
        }
    }
}
