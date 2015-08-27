using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class MetadataTypeCatalog
    {
        [ThreadStatic]
        private static object[] objectArray2;

        [ThreadStatic]
        private static object[] objectArray3;

        [ThreadStatic]
        private static object[] objectArray4;

        [ThreadStatic]
        private static object[] objectArray5;

        private void SetExports(Type type, ICompositionElement creationInfo)
        {
            var exports = new List<ExportDefinition>();
            var contractNamesOnNonInterfaces = new HashSet<string>();
            Initialize2();
            Initialize3();
            Initialize4();
            Initialize5();

            foreach (var member in metadataInfo.GetExportedMembers(type))
            {
                foreach (ExportAttribute exportAttribute in member.GetAttributes<ExportAttribute>())
                {
                    string contractName = null;
                    Type typeIdentityType = null;
                    objectArray4[0] = member;
                    objectArray4[1] = exportAttribute;
                    objectArray4[2] = typeIdentityType;
                    objectArray4[3] = contractName;
                    getContractInfoFromExport.Invoke(null, objectArray4);
                    typeIdentityType = (Type)objectArray4[2];
                    contractName = (string)objectArray4[3];

                    objectArray5[0] = creationInfo;
                    objectArray5[1] = member;
                    objectArray5[2] = exportAttribute;
                    objectArray5[3] = typeIdentityType;
                    objectArray5[4] = contractName;
                    var attributedExportDefinition = (ExportDefinition)
                        attributedExportDefinitionCtor.Invoke(objectArray5);

                    bool add = true;

                    if (exportAttribute.GetType() == typeofInheritedExport)
                    {
                        // Any InheritedExports on the type itself are contributed during this pass 
                        // and we need to do the book keeping for those.
                        if (!contractNamesOnNonInterfaces.Contains(contractName))
                        {
                            contractNamesOnNonInterfaces.Add(contractName);
                        }
                        else
                        {
                            add = false;
                        }
                    }

                    if (add)
                    {
                        objectArray3[0] = ToLazyMember(member);
                        objectArray3[1] = attributedExportDefinition;
                        objectArray3[2] = creationInfo;
                        ExportDefinition reflectionMemberExportDefinition =
                            (ExportDefinition)reflectionMemberExportDefinitionCtor.Invoke(objectArray3);

                        exports.Add(reflectionMemberExportDefinition);
                    }
                }
            }

            // GetInheritedExports should only contain InheritedExports on base types or interfaces.
            // The order of types returned here is important because it is used as a priority list
            // of which InheritedExport to choose if multiple exists with the same contract name.
            // Therefore ensure that we always return the types in the hierarchy from most derived
            // to the lowest base type, followed by all the interfaces that this type implements.
            foreach (Type t in GetInheritedExports(type))
            {
                foreach (InheritedExportAttribute exportAttribute in t.GetAttributes<InheritedExportAttribute>(true))
                {
                    objectArray2[0] = t;
                    objectArray2[1] = exportAttribute;
                    var attributedExportDefinition = (ExportDefinition)
                        attributedPartCreationInfoCreateExportDefinition.Invoke(creationInfo, objectArray2);

                    if (!contractNamesOnNonInterfaces.Contains(attributedExportDefinition.ContractName))
                    {
                        objectArray3[0] = ToLazyMember(t);
                        objectArray3[1] = attributedExportDefinition;
                        objectArray3[2] = creationInfo;
                        ExportDefinition reflectionMemberExportDefinition =
                            (ExportDefinition)reflectionMemberExportDefinitionCtor.Invoke(objectArray3);

                        exports.Add(reflectionMemberExportDefinition);

                        if (!t.IsInterface)
                        {
                            contractNamesOnNonInterfaces.Add(attributedExportDefinition.ContractName);
                        }
                    }
                }
            }

            attributedPartCreationInfoExports.SetValue(creationInfo, exports);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize2()
        {
            if (objectArray2 == null)
            {
                objectArray2 = new object[2];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize3()
        {
            if (objectArray3 == null)
            {
                objectArray3 = new object[3];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize4()
        {
            if (objectArray4 == null)
            {
                objectArray4 = new object[4];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize5()
        {
            if (objectArray5 == null)
            {
                objectArray5 = new object[5];
            }
        }

        private IEnumerable<Type> GetInheritedExports(Type type)
        {
            // If the type is abstract we aren't interested in type level exports
            if (type.IsAbstract)
            {
                yield break;
            }

            // The order of types returned here is important because it is used as a priority list
            // of which InheritedExport to choose if multiple exists with the same contract name.
            // Therefore ensure that we always return the types in the hierarchy from most derived
            // to the lowest base type, followed by all the interfaces that this type implements.

            Type currentType = type.BaseType;

            if (currentType == null)
            {
                yield break;
            }

            // Stopping at object instead of null to help with performance. It is a noticeable
            // performance gain (~5%) if we don't have to try and pull the attributes we know don't
            // exist on object. We also need the null check in case we're passed a type that doesn't
            // live in the runtime context.
            while (currentType != null && currentType != typeofObject)
            {
                if (IsInheritedExport(currentType))
                {
                    yield return currentType;
                }

                currentType = currentType.BaseType;
            }

            foreach (Type iface in type.GetInterfaces())
            {
                if (IsInheritedExport(iface))
                {
                    yield return iface;
                }
            }
        }

        private static bool IsInheritedExport(ICustomAttributeProvider attributedProvider)
        {
            return attributedProvider.IsAttributeDefined<InheritedExportAttribute>(false);
        }

        private static LazyMemberInfo ToLazyMember(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo property = member as PropertyInfo;
                MemberInfo[] accessors = new MemberInfo[]
                {
                    property.GetGetMethod(true),
                    property.GetSetMethod(true)
                };
                return new LazyMemberInfo(MemberTypes.Property, accessors);
            }
            else
            {
                return new LazyMemberInfo(member);
            }
        }
    }
}