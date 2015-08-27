using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TestComposition
    {
        [TestMethod]
        public void TestCodeFlowComposition()
        {
            string[] componentAssemblyFiles = new string[]
            {
                "Microsoft.Language.Xml.Editor.dll",
                "Microsoft.VisualStudio.CoreUtility.dll",
                "Microsoft.VisualStudio.Language.Intellisense.dll",
                "Microsoft.VisualStudio.Language.StandardClassification.dll",
                "Microsoft.VisualStudio.Text.Data.dll",
                "Microsoft.VisualStudio.Text.Internal.dll",
                "Microsoft.VisualStudio.Text.Logic.dll",
                "Microsoft.VisualStudio.Text.UI.dll",
                "Microsoft.VisualStudio.Text.UI.Wpf.dll",
            };

            var directory = Environment.CurrentDirectory;

            var catalogs = componentAssemblyFiles
                .Select(f => new AssemblyCatalog(Path.Combine(directory, f)))
                .ToArray();
            var elapsed = CompositionDumper.TimeComposition(catalogs, @"E:\1.txt");
        }
    }
}
