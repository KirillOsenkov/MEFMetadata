using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Metadata = Microsoft.Composition.Metadata;

namespace System.ComponentModel.Composition.Hosting
{
    public class MetadataInfo : IMetadataInfo
    {
        private static ConditionalWeakTable<Task<Metadata.AssemblyCatalogInfo>, MetadataInfo> assemblyCatalogInfoToMetadataInfoMap = new ConditionalWeakTable<Task<Metadata.AssemblyCatalogInfo>, MetadataInfo>();
        private static Metadata.Discovery discovery = null;

        private Task<Metadata.AssemblyCatalogInfo> assemblyCatalogInfo;
        private Assembly assembly;
        private Dictionary<Type, Metadata.TypeInfo> typeInfoMap;

        public MetadataInfo(Task<Metadata.AssemblyCatalogInfo> assemblyCatalogInfo, Assembly assembly = null)
        {
            this.assemblyCatalogInfo = assemblyCatalogInfo;
            this.assembly = assembly;
        }

        public Assembly Assembly
        {
            get
            {
                if (assembly == null && assemblyCatalogInfo.Result != null)
                {
                    assembly = Metadata.AssemblyLoader.LoadAssembly(assemblyCatalogInfo.Result.AssemblyFilePath);
                }

                return assembly;
            }
        }

        public Module Module
        {
            get
            {
                return Assembly.ManifestModule;
            }
        }

        public string AssemblyFullName
        {
            get
            {
                if (assemblyCatalogInfo.Result == null)
                {
                    return null;
                }

                return assemblyCatalogInfo.Result.FullAssemblyName;
            }
        }

        public IEnumerable<Type> ExportedTypes
        {
            get
            {
                InitializeTypeInfoMap();
                return typeInfoMap.Where(kvp => kvp.Value.IsExported).Select(kvp => kvp.Key);
            }
        }

        public async Task Realize()
        {
            await assemblyCatalogInfo.ConfigureAwait(false);
        }

        private void InitializeTypeInfoMap()
        {
            lock (this)
            {
                if (typeInfoMap != null)
                {
                    return;
                }

                typeInfoMap = new Dictionary<Type, Metadata.TypeInfo>();
                if (assemblyCatalogInfo.Result == null)
                {
                    return;
                }

                var typeInfos = assemblyCatalogInfo.Result.CompositionTypes;
                foreach (var typeInfo in typeInfos)
                {
                    var type = Module.ResolveType(typeInfo.MetadataToken);
                    typeInfoMap.Add(type, typeInfo);
                }
            }
        }

        public static Metadata.FolderBasedAssemblyResolver AssemblyResolver = new Metadata.FolderBasedAssemblyResolver();

        public static Metadata.Discovery Discovery
        {
            get
            {
                if (discovery == null)
                {
                    discovery = new Metadata.Discovery(AssemblyResolver);
                }

                return discovery;
            }
        }

        public static IMetadataInfo GetOrCreate(string assemblyFilePath)
        {
            lock (assemblyCatalogInfoToMetadataInfoMap)
            {
                MetadataInfo result;

                AssemblyResolver.AddPath(Path.GetFullPath(Path.GetDirectoryName(assemblyFilePath)));

                var assemblyCatalogInfo = Discovery.GetAssemblyCatalogInfoFromFile(assemblyFilePath);
                if (assemblyCatalogInfo == null)
                {
                    throw new FileNotFoundException(
                        string.Format("Could not resolve assembly {0} likely because the file wasn't found on disk.", assemblyFilePath));
                }

                if (!assemblyCatalogInfoToMetadataInfoMap.TryGetValue(assemblyCatalogInfo, out result))
                {
                    result = new MetadataInfo(assemblyCatalogInfo);
                    assemblyCatalogInfoToMetadataInfoMap.Add(assemblyCatalogInfo, result);
                }

                return result;
            }
        }

        public static IMetadataInfo GetOrCreateByAssemblyFullName(string assemblyFullName)
        {
            if (Discovery.IsKnownNonMefAssembly(assemblyFullName))
            {
                return null;
            }

            lock (assemblyCatalogInfoToMetadataInfoMap)
            {
                MetadataInfo result;

                var assemblyCatalogInfo = Discovery.GetAssemblyCatalogInfoFromAssemblyName(assemblyFullName);
                if (!assemblyCatalogInfoToMetadataInfoMap.TryGetValue(assemblyCatalogInfo, out result))
                {
                    result = new MetadataInfo(assemblyCatalogInfo);
                    assemblyCatalogInfoToMetadataInfoMap.Add(assemblyCatalogInfo, result);
                }

                return result;
            }
        }

        public static IMetadataInfo GetOrCreate(Assembly assembly)
        {
            lock (assemblyCatalogInfoToMetadataInfoMap)
            {
                MetadataInfo result;

                var assemblyCatalogInfo = Discovery.GetAssemblyCatalogInfoFromFile(assembly.Location);
                if (!assemblyCatalogInfoToMetadataInfoMap.TryGetValue(assemblyCatalogInfo, out result))
                {
                    result = new MetadataInfo(assemblyCatalogInfo, assembly);
                    assemblyCatalogInfoToMetadataInfoMap.Add(assemblyCatalogInfo, result);
                }

                return result;
            }
        }

        public IEnumerable<MemberInfo> GetExportedMembers(Type type)
        {
            InitializeTypeInfoMap();

            Metadata.TypeInfo typeInfo = null;
            if (!typeInfoMap.TryGetValue(type, out typeInfo))
            {
                return Enumerable.Empty<MemberInfo>();
            }

            var result = new HashSet<MemberInfo>();
            result.Add(type);
            ResolveMemberInfo(typeInfo.ExportedMembers, result);
            return result;
        }

        public IEnumerable<MemberInfo> GetImportedMembers(Type type)
        {
            InitializeTypeInfoMap();

            Metadata.TypeInfo typeInfo = null;
            if (!typeInfoMap.TryGetValue(type, out typeInfo) || !typeInfo.HasImportedMembers)
            {
                return Enumerable.Empty<MemberInfo>();
            }

            return ResolveMemberInfo(typeInfo.ImportedMembers);
        }

        private IEnumerable<MemberInfo> ResolveMemberInfo(IEnumerable<Metadata.MemberInfo> memberInfoList, HashSet<MemberInfo> result = null)
        {
            if (result == null)
            {
                result = new HashSet<MemberInfo>();
            }

            foreach (var member in memberInfoList)
            {
                MemberInfo memberInfo = null;
                switch (member.Kind)
                {
                    case Metadata.MemberKind.Property:
                        memberInfo = GetPropertyInfo(member);
                        break;
                    case Metadata.MemberKind.Method:
                        memberInfo = GetMethodInfo(member);
                        break;
                    case Metadata.MemberKind.Field:
                        memberInfo = GetFieldInfo(member);
                        break;
                    default:
                        break;
                }

                result.Add(memberInfo);
            }

            return result;
        }

        private MemberInfo GetFieldInfo(Metadata.MemberInfo memberInfo)
        {
            return Module.ResolveField(memberInfo.Token);
        }

        private MemberInfo GetMethodInfo(Metadata.MemberInfo memberInfo)
        {
            return Module.ResolveMethod(memberInfo.Token);
        }

        private MemberInfo GetPropertyInfo(Metadata.MemberInfo memberInfo)
        {
            return ReflectionHelpers.GetPropertyInfo(this.Module, memberInfo.Token, memberInfo.Type.MetadataToken);
        }
    }
}
