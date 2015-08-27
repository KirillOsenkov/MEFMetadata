using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Composition.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TestMetadataInfo
    {
        [TestMethod]
        public void TestMetadataInfo1()
        {
            var metadataInfo = MetadataInfo.GetOrCreate(@"StyleCop.dll");
            var types = metadataInfo.ExportedTypes.ToArray();
        }
    }
}
