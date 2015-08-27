using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class MetadataTypeCatalog
    {
        private void SetImports(Type type, ICompositionElement creationInfo)
        {
            IEnumerable<ImportDefinition> imports = GetImportMembers(type, creationInfo);

            var constructor = attributedPartCreationInfoGetConstructor.Invoke(creationInfo, null) as ConstructorInfo;
            if (constructor != null)
            {
                List<ImportDefinition> importsList = imports as List<ImportDefinition> ?? new List<ImportDefinition>();

                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    ImportDefinition importDefinition =
                        attributedModelDiscoveryCreateParameterImportDefinitionDelegate
                            (parameter, creationInfo);
                    importsList.Add(importDefinition);
                }

                imports = importsList;
            }

            attributedPartCreationInfoImports.SetValue(creationInfo, imports);
        }

        private IEnumerable<ImportDefinition> GetImportMembers(Type type, ICompositionElement creationInfo)
        {
            if (type == typeofObject)
            {
                return Enumerable.Empty<ImportDefinition>();
            }

            var originalType = type;
            var results = new List<ImportDefinition>();

            do
            {
                bool shouldBreak = false;
                var members = GetDeclaredOnlyImportMembers(type, out shouldBreak);
                if (shouldBreak)
                {
                    break;
                }
#if !PARALLEL
                foreach (var member in members)
                {
                    var importDefinition = CreateImportDefinition(type, member, creationInfo);
                    results.Add(importDefinition);
                }
#else
                Parallel.ForEach(members, new ParallelOptions { MaxDegreeOfParallelism = 1 }, member =>
                {
                    var importDefinition = CreateImportDefinition(type, member, creationInfo);
                    lock (results)
                    {
                        results.Add(importDefinition);
                    }
                });
#endif

                type = type.BaseType;
            }
            while (type != null && type != typeofObject);

            return results;
        }

        private IEnumerable<MemberInfo> GetDeclaredOnlyImportMembers(Type type, out bool shouldBreak)
        {
            shouldBreak = false;

            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            var metadata = metadataInfo;
            var assemblyFullName = type.Assembly.FullName;
            if (assemblyFullName != metadataInfo.AssemblyFullName)
            {
                metadata = MetadataTypeCatalog.GetOrCreateMetadataInfoByAssemblyFullName(assemblyFullName);
                if (metadata == null)
                {
                    shouldBreak = true;
                    return Enumerable.Empty<MemberInfo>();
                }
            }

            return metadata.GetImportedMembers(type);
        }

        private ImportDefinition CreateImportDefinition(Type realDeclaringType, MemberInfo member, ICompositionElement creationInfo)
        {
            if (member.DeclaringType.ContainsGenericParameters)
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    member = realDeclaringType.GetField(member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                else
                {
                    member = realDeclaringType.GetProperty(member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (member == null)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }

            ImportDefinition importDefinition =
                attributedModelDiscoveryCreateMemberImportDefinitionDelegate
                    (member, creationInfo);

            return importDefinition;
        }
    }
}