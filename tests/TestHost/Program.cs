using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.Composition.Metadata;

namespace TestHost
{
    [Export]
    class Program
    {
        [Import]
        public Foo Foo { get; set; }

        [Export]
        public Foo ExportedProperty { get; set; }

        public bool UseMetadata = true;

        private static readonly string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string compositionCacheFile = Path.Combine(currentDirectory, "VSMef.Composition.cache");

        [STAThread]
        static void Main(string[] args)
        {
            EnableProfileOptimization();

            using (Timing("Everything"))
            {
                LoadAssemblies();
                new Program().Compose();
            }
        }

        public static IDisposable Timing(string message)
        {
            return new DisposableTiming(message);
        }

        private static void LoadAssemblies()
        {
            using (Timing("Loading assemblies"))
            {
                foreach (var assemblyFilePath in GetAssemblies())
                {
                    Task.Run(() =>
                    {
                        AssemblyLoader.LoadAssembly(assemblyFilePath);
                    });
                }
            }
        }

        private static void EnableProfileOptimization()
        {
            ProfileOptimization.SetProfileRoot(currentDirectory);
            ProfileOptimization.StartProfile("Startup.profile");
        }

        private void CreateMetadata()
        {
            using (Timing("Metadata"))
            {
                string[] componentAssemblyFiles = GetAssemblies();

                //Parallel.ForEach(filePaths, filePath =>
                //{
                //    var info = MetadataInfo.GetOrCreate(filePath);
                //    var types = info.ExportedTypes;
                //});

                var list = new List<Task>();
                foreach (var filePath in componentAssemblyFiles)
                {
                    list.Add(Task.Run(() =>
                {
                    var info = MetadataInfo.GetOrCreate(filePath);
                    var types = info.ExportedTypes;
                }
                    ));
                }

                Task.WaitAll(list.ToArray());
            }
        }

        //private void ComposeVsMEF()
        //{
        //    CreateVSMefCache();

        //    using (Timing("VSMef"))
        //    {
        //        var cacheManager = new CachedComposition();
        //        IExportProviderFactory exportProviderFactory;
        //        using (var cacheStream = File.OpenRead(compositionCacheFile))
        //        {
        //            exportProviderFactory = cacheManager.LoadExportProviderFactoryAsync(cacheStream).GetAwaiter().GetResult();
        //        }

        //        var exportProvider = exportProviderFactory.CreateExportProvider();
        //        var part = exportProvider.GetExportedValue<IMainWindowViewModel>();
        //    }
        //}

        //private static void CreateVSMefCache()
        //{
        //    if (File.Exists(compositionCacheFile))
        //    {
        //        return;
        //    }

        //    using (Timing("Create MEF cache"))
        //    {
        //        var createCompositionTask = new CreateComposition();
        //        createCompositionTask.CatalogAssemblies = GetAssemblies().Select(s => new TaskItem(s)).ToArray();
        //        createCompositionTask.CompositionCacheFile = compositionCacheFile;
        //        createCompositionTask.BuildEngine = new BuildEngine();
        //        createCompositionTask.Execute();
        //    }
        //}

        public static void Log(string elapsed)
        {
            File.AppendAllText("D:\\elapsed.txt", elapsed + Environment.NewLine);
        }

        private void Compose()
        {
            string[] componentAssemblyFiles = GetAssemblies();

            var catalogs = componentAssemblyFiles
                .Select(f => new MetadataAssemblyCatalog(f))
                .ToArray();

            using (Timing("Composition"))
            {
#if true
                CompositionDumper.TimeComposition(catalogs, "D:\\1.txt");
#else
                CompositionDumper.TimeComposition(catalogs);
#endif
            }
        }

        private static string[] GetAssemblies()
        {
            string[] componentAssemblyFiles = new string[]
            {
                //"Microsoft.VisualStudio.Text.Logic.dll",
                //"Microsoft.VisualStudio.Text.UI.dll",
                //"Microsoft.VisualStudio.Text.UI.Wpf.dll",
                //"Microsoft.Language.Xml.Editor.dll",
                "Microsoft.VisualStudio.Editor.Implementation, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            };

            return componentAssemblyFiles.Select(s => Assembly.Load(s).Location).ToArray();
        }

        private AggregateCatalog CreateCatalog()
        {
            var catalog = new AggregateCatalog();
            return catalog;
        }

        protected ComposablePartCatalog CreateAssemblyCatalog(string assemblyFile)
        {
            ComposablePartCatalog result = null;
            result = new MetadataAssemblyCatalog(assemblyFile);

            return result;
        }
    }

    //public class ImplementedExportTest : IImplementedExportTest
    //{
    //}

    [ExportCompletionProvider]
    internal abstract class Foo
    {
        [ImportingConstructor]
        public Foo()
        {
        }
    }

    [InheritedExport]
    public interface IImplementedExportA
    {
    }

    public class ImplementedExportB : IImplementedExportA
    {
    }

    [InheritedExport]
    public class InheritedExportA
    {
    }

    public class InheritedExportB : InheritedExportA
    {
    }

    //[ExportCompletionProvider]
    //internal class Foo2 : Foo
    //{
    //    [ImportingConstructor]
    //    public Foo2()
    //    {
    //    }
    //}

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportCompletionProviderAttribute : ExportAttribute
    {
        public ExportCompletionProviderAttribute()
            : base(typeof(Foo))
        {
        }
    }
}
