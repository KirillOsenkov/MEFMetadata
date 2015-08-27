using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Composition.Metadata
{
    public class FolderBasedAssemblyResolver : IAssemblyResolver
    {
        private HashSet<string> assemblyFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ConcurrentDictionary<string, string> assemblyResolutionCache = new ConcurrentDictionary<string, string>();

        public FolderBasedAssemblyResolver(params string[] folders)
        {
            foreach (var folder in folders)
            {
                assemblyFolders.Add(folder);
            }
        }

        public string ResolveAssembly(string assemblyName)
        {
            string assemblyFilePath = null;

            if (assemblyResolutionCache.TryGetValue(assemblyName, out assemblyFilePath))
            {
                return assemblyFilePath;
            }

            var shortAssemblyName = assemblyName;
            int firstComma = shortAssemblyName.IndexOf(',');
            if (firstComma > -1)
            {
                shortAssemblyName = shortAssemblyName.Substring(0, firstComma);
            }

            foreach (var folder in assemblyFolders)
            {
                var candidateFilePath = Path.Combine(folder, shortAssemblyName + ".dll");
                if (File.Exists(candidateFilePath))
                {
                    assemblyResolutionCache.TryAdd(assemblyName, candidateFilePath);
                    return candidateFilePath;
                }
            }

            try
            {
                var assembly = AssemblyLoader.LoadAssemblyByName(assemblyName);
                assemblyFilePath = assembly.Location;
            }
            catch (Exception)
            {
                // just return null if we failed to load - they'll deal with null as failure
            }

            return assemblyFilePath;
        }

        public void AddPath(string directoryPath)
        {
            assemblyFolders.Add(directoryPath);
        }
    }
}
