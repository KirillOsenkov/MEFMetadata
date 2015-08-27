using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace Microsoft.Composition.Metadata
{
    public class Discovery
    {
        private readonly IAssemblyResolver assemblyResolver;
        private readonly Dictionary<string, Task<AssemblyCatalogInfo>> cache = new Dictionary<string, Task<AssemblyCatalogInfo>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> isMefAssemblyCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "Accessibility", false },
            { "LibGit2Sharp", false },
            { "Microsoft.Build.Framework", false },
            { "Microsoft.Build.Tasks.v4.0", false },
            { "Microsoft.Build.Utilities.v4.0", false },
            { "Microsoft.JScript", false },
            { "Microsoft.Language.Xml", false },
            { "Microsoft.Transactions.Bridge", false },
            { "Microsoft.VisualBasic", false },
            { "Microsoft.VisualBasic.Activities.Compiler", false },
            { "Microsoft.VisualStudio.CoreUtility", false },
            { "Microsoft.VisualStudio.Language.Intellisense", false },
            { "Microsoft.VisualStudio.Language.StandardClassification", false },
            { "Microsoft.VisualStudio.Text.Data", false },
            { "Microsoft.VisualStudio.Text.Internal", true }, // this one is needed
            { "mscorlib", false },
            { "PresentationCore", false },
            { "PresentationFramework", false },
            { "SMDiagnostics", false },
            { "System", false },
            { "System.Activities", false },
            { "System.Activities.DurableInstancing", false },
            { "System.ComponentModel.Composition", false },
            { "System.ComponentModel.Composition.MetadataCatalog", false },
            { "System.ComponentModel.DataAnnotations", false },
            { "System.Configuration", false },
            { "System.Configuration.Install", false },
            { "System.Core", false },
            { "System.Data", false },
            { "System.Data.OracleClient", false },
            { "System.Data.SqlXml", false },
            { "System.Deployment", false },
            { "System.Design", false },
            { "System.DirectoryServices", false },
            { "System.DirectoryServices.Protocols", false },
            { "System.Drawing", false },
            { "System.Drawing.Design", false },
            { "System.EnterpriseServices", false },
            { "System.IdentityModel", false },
            { "System.IdentityModel.Selectors", false },
            { "System.IO.Compression", false },
            { "System.IO.Compression.FileSystem", false },
            { "System.Management", false },
            { "System.Messaging", false },
            { "System.Net.Http", false },
            { "System.Numerics", false },
            { "System.Runtime", false },
            { "System.Runtime.Caching", false },
            { "System.Runtime.DurableInstancing", false },
            { "System.Runtime.Remoting", false },
            { "System.Runtime.Serialization", false },
            { "System.Runtime.Serialization.Formatters.Soap", false },
            { "System.ServiceModel", false },
            { "System.ServiceModel.Activation", false },
            { "System.ServiceModel.Internals", false },
            { "System.ServiceProcess", false },
            { "System.Security", false },
            { "System.Transactions", false },
            { "System.Web", false },
            { "System.Web.ApplicationServices", false },
            { "System.Web.RegularExpressions", false },
            { "System.Web.Services", false },
            { "System.Windows.Forms", false },
            { "System.Windows.Input.Manipulations", false },
            { "System.Xaml", false },
            { "System.Xml", false },
            { "System.Xml.Linq", false },
            { "UIAutomationClient", false },
            { "UIAutomationProvider", false },
            { "UIAutomationTypes", false },
            { "WindowsBase", false },
        };

        public Discovery(IAssemblyResolver assemblyResolver)
        {
            this.assemblyResolver = assemblyResolver;
        }

        public bool IsKnownNonMefAssembly(string assemblyName)
        {
            var partialName = assemblyName;
            int index = assemblyName.IndexOf(',');
            if (index > -1)
            {
                partialName = assemblyName.Substring(0, index);
            }

            lock (isMefAssemblyCache)
            {
                bool isPotentiallyMef = true;
                return isMefAssemblyCache.TryGetValue(partialName, out isPotentiallyMef) && !isPotentiallyMef;
            }
        }

        public Task<AssemblyCatalogInfo> GetAssemblyCatalogInfoFromAssemblyName(string assemblyName)
        {
            if (IsKnownNonMefAssembly(assemblyName))
            {
                return null;
            }

            var assemblyFilePath = ResolveAssemblyByName(assemblyName);
            if (assemblyFilePath == null)
            {
                return null;
            }

            return GetAssemblyCatalogInfoFromFile(assemblyFilePath);
        }

        public Task<AssemblyCatalogInfo> GetAssemblyCatalogInfoFromFile(string assemblyFilePath)
        {
            Task<AssemblyCatalogInfo> result = null;

            lock (cache)
            {
                if (cache.TryGetValue(assemblyFilePath, out result))
                {
                    return result;
                }

                result = Task.Run(() => GetAssemblyCatalogInfoCore(assemblyFilePath));
                cache.Add(assemblyFilePath, result);
                return result;
            }
        }

        private string ResolveAssemblyByName(string assemblyName)
        {
            return assemblyResolver.ResolveAssembly(assemblyName);
        }

        private async Task<AssemblyCatalogInfo> GetAssemblyCatalogInfoCore(string assemblyFilePath)
        {
            AssemblyCatalogInfo result = null;

            try
            {
                using (var stream = new FileStream(
                    assemblyFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read | FileShare.Delete,
                    262144,
                    FileOptions.RandomAccess))
                {
                    using (var pereader = new PEReader(stream, PEStreamOptions.LeaveOpen))
                    {
                        if (!pereader.HasMetadata)
                        {
                            return null;
                        }

                        var metadataReader = pereader.GetMetadataReader();
                        if (!metadataReader.ReferencesAssembly("System.ComponentModel.Composition"))
                        {
                            return null;
                        }

                        result = new AssemblyCatalogInfo(this, metadataReader, assemblyFilePath);
                        await result.Populate().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }
    }
}
