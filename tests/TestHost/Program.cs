using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
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

        public class TaskItem : ITaskItem
        {
            public TaskItem(string itemSpec)
            {
                this.ItemSpec = itemSpec;
            }

            public string ItemSpec { get; set; }

            public int MetadataCount
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ICollection MetadataNames
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IDictionary CloneCustomMetadata()
            {
                throw new NotImplementedException();
            }

            public void CopyMetadataTo(ITaskItem destinationItem)
            {
                throw new NotImplementedException();
            }

            public string GetMetadata(string metadataName)
            {
                throw new NotImplementedException();
            }

            public void RemoveMetadata(string metadataName)
            {
                throw new NotImplementedException();
            }

            public void SetMetadata(string metadataName, string metadataValue)
            {
                throw new NotImplementedException();
            }
        }

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

        public class DisposableTiming : IDisposable
        {
            private string message;
            private Stopwatch sw;

            public DisposableTiming(string message)
            {
                this.sw = Stopwatch.StartNew();
                this.message = message;
            }

            public void Dispose()
            {
                Console.WriteLine(message + ": " + sw.Elapsed.ToString("s\\.fff"));
                if (message == "Everything")
                {
                    Log(sw.Elapsed.ToString("s\\.fff"));
                }
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

        public class BuildEngine : IBuildEngine
        {
            public int ColumnNumberOfTaskNode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool ContinueOnError
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int LineNumberOfTaskNode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string ProjectFileOfTaskNode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            {
                throw new NotImplementedException();
            }

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
                throw new NotImplementedException();
            }

            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                throw new NotImplementedException();
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
                Console.WriteLine(e.Message);
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
                throw new NotImplementedException();
            }
        }

        private static void Log(string elapsed)
        {
            File.AppendAllText("E:\\elapsed.txt", elapsed + Environment.NewLine);
        }

        private void Compose()
        {
            string[] componentAssemblyFiles = GetAssemblies();

            var catalogs = componentAssemblyFiles
                .Select(f => new MetadataAssemblyCatalog(f))
                .ToArray();

            using (Timing("Composition"))
            {
#if false
                CompositionDumper.TimeComposition(catalogs, "E:\\2.txt");
#else
                CompositionDumper.TimeComposition(catalogs);
#endif
            }
        }

        private static string[] GetAssemblies()
        {
            string[] componentAssemblyFiles = new string[]
            {
                "Microsoft.VisualStudio.Text.Logic.dll",
                "Microsoft.VisualStudio.Text.UI.dll",
                "Microsoft.VisualStudio.Text.UI.Wpf.dll",
                "Microsoft.Language.Xml.Editor.dll",
            };

            return componentAssemblyFiles.Select(s => Path.Combine(Environment.CurrentDirectory, s)).ToArray();
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
