using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace Microsoft.Composition.Metadata
{
    public class AssemblyCatalogInfo
    {
        private Discovery discovery;
        private MetadataReader metadataReader;
        private string assemblyFilePath;
        private string fullAssemblyName;

        private Dictionary<TypeHandle, TypeInfo> compositionTypes = new Dictionary<TypeHandle, TypeInfo>();
        private Dictionary<FieldHandle, MemberInfo> fields;
        private Dictionary<PropertyHandle, MemberInfo> properties;
        private Dictionary<MethodHandle, MemberInfo> methods;

        private readonly Dictionary<Handle, bool> isImportAttributeCache = new Dictionary<Handle, bool>();
        private readonly Dictionary<Handle, ExportOrInheritedExport> isExportAttributeCache = new Dictionary<Handle, ExportOrInheritedExport>();
        private HashSet<string> knownExportAttributeOrDerivedType;
        private HashSet<string> knownImportAttributeOrDerivedType;

        //private bool hasInheritedExports = false;
        //private readonly Dictionary<Handle, bool> inheritedExportTypes = new Dictionary<Handle, bool>();
        //private readonly HashSet<string> inheritedExportTypesByName = new HashSet<string>();

        public AssemblyCatalogInfo(Discovery discovery, MetadataReader metadataReader, string assemblyFilePath)
        {
            Debug.WriteLine("AssemblyCatalogInfo: " + assemblyFilePath);
            this.discovery = discovery;
            this.metadataReader = metadataReader;
            this.assemblyFilePath = assemblyFilePath;
        }

        public string AssemblyFilePath
        {
            get { return this.assemblyFilePath; }
        }

        public string FullAssemblyName
        {
            get
            {
                return fullAssemblyName;
            }
        }

        public bool IsMefAssembly { get; private set; }

        public IEnumerable<TypeInfo> CompositionTypes
        {
            get
            {
                return compositionTypes.Values;
            }
        }

        public HashSet<string> KnownExportOrDerived
        {
            get
            {
                return this.knownExportAttributeOrDerivedType;
            }
        }

        public HashSet<string> KnownImportOrDerived
        {
            get
            {
                return this.knownImportAttributeOrDerivedType;
            }
        }

        public async Task Populate()
        {
            this.IsMefAssembly = true;
            this.fullAssemblyName = this.metadataReader.GetFullAssemblyName();

            this.knownExportAttributeOrDerivedType = new HashSet<string>()
            {
                "System.ComponentModel.Composition.ExportAttribute",
                "System.ComponentModel.Composition.InheritedExportAttribute"
            };
            this.knownImportAttributeOrDerivedType = new HashSet<string>()
            {
                "System.ComponentModel.Composition.ImportAttribute",
                "System.ComponentModel.Composition.ImportManyAttribute"
            };

            var referenceFullNames = metadataReader.GetReferenceAssemblyFullNames();
            var tasks = new List<Task<AssemblyCatalogInfo>>();
            foreach (var referenceAssemblyFullName in referenceFullNames)
            {
                var referenceCatalog = this.discovery.GetAssemblyCatalogInfoFromAssemblyName(referenceAssemblyFullName);
                if (referenceCatalog != null)
                {
                    tasks.Add(referenceCatalog);
                }
            }

            foreach (var task in tasks)
            {
                var referenceCatalog = await task.ConfigureAwait(false);
                if (referenceCatalog == null)
                {
                    continue;
                }

                var knownExportAttributes = referenceCatalog.KnownExportOrDerived;
                foreach (var known in knownExportAttributes)
                {
                    knownExportAttributeOrDerivedType.Add(known);
                }

                var knownImportAttributes = referenceCatalog.KnownImportOrDerived;
                foreach (var known in knownImportAttributes)
                {
                    knownImportAttributeOrDerivedType.Add(known);
                }
            }

            foreach (var typeHandle in metadataReader.TypeDefinitions)
            {
                var typeDefinition = metadataReader.GetTypeDefinition(typeHandle);
                Walk(typeDefinition);
            }

            foreach (var customAttributeHandle in metadataReader.CustomAttributes)
            {
                var customAttribute = metadataReader.GetCustomAttribute(customAttributeHandle);
                var attributeTypeHandle = metadataReader.GetAttributeTypeHandle(customAttribute);

                if (TryHandleImportAttribute(customAttribute, attributeTypeHandle))
                {
                    continue;
                }

                if (TryHandleExportAttribute(customAttribute, attributeTypeHandle))
                {
                    continue;
                }
            }
        }

        private void Walk(TypeDefinition typeDefinition)
        {
            var baseTypeHandle = typeDefinition.BaseType;
            if (baseTypeHandle.IsNil)
            {
                return;
            }

            var handleType = baseTypeHandle.HandleType;
            if (handleType == HandleType.Type)
            {
                typeDefinition = metadataReader.GetTypeDefinition((TypeHandle)baseTypeHandle);
                var typeFullName = metadataReader.GetFullTypeName(typeDefinition);
                if (knownExportAttributeOrDerivedType.Contains(typeFullName))
                {
                    typeFullName = metadataReader.GetFullTypeName(typeDefinition);
                    knownExportAttributeOrDerivedType.Add(typeFullName);
                    //isExportAttributeCache[baseTypeHandle] = ExportOrInheritedExport.Export;
                    return;
                }
                else if (knownImportAttributeOrDerivedType.Contains(typeFullName))
                {
                    typeFullName = metadataReader.GetFullTypeName(typeDefinition);
                    knownImportAttributeOrDerivedType.Add(typeFullName);
                    //isImportAttributeCache[baseTypeHandle] = true;
                    return;
                }

                Walk(typeDefinition);
            }
            else if (handleType == HandleType.TypeReference)
            {
                TypeReference typeReference = metadataReader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                var typeFullName = metadataReader.GetFullTypeName(typeReference);
                if (knownExportAttributeOrDerivedType.Contains(typeFullName))
                {
                    typeFullName = metadataReader.GetFullTypeName(typeDefinition);
                    knownExportAttributeOrDerivedType.Add(typeFullName);
                    //isExportAttributeCache[baseTypeHandle] = ExportOrInheritedExport.Export;
                    return;
                }
                else if (knownImportAttributeOrDerivedType.Contains(typeFullName))
                {
                    typeFullName = metadataReader.GetFullTypeName(typeDefinition);
                    knownImportAttributeOrDerivedType.Add(typeFullName);
                    //isImportAttributeCache[baseTypeHandle] = true;
                    return;
                }
            }
            else if (handleType == HandleType.TypeSpecification)
            {
                // TODO: not implemented
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private bool IsImportAttribute(Handle attributeTypeHandle)
        {
            bool isImport = false;

            if (isImportAttributeCache.TryGetValue(attributeTypeHandle, out isImport))
            {
                return isImport;
            }

            isImport = IsImportOrImportManyAttribute(attributeTypeHandle);
            isImportAttributeCache[attributeTypeHandle] = isImport;
            return isImport;
        }

        private bool IsExportAttribute(Handle attributeTypeHandle, out bool isInheritedExport)
        {
            ExportOrInheritedExport exportOrInheritedExport;

            if (!isExportAttributeCache.TryGetValue(attributeTypeHandle, out exportOrInheritedExport))
            {
                exportOrInheritedExport = IsExportOrInheritedExportAttribute(attributeTypeHandle);
                isExportAttributeCache.Add(attributeTypeHandle, exportOrInheritedExport);
            }

            isInheritedExport = exportOrInheritedExport == ExportOrInheritedExport.InheritedExport;
            return exportOrInheritedExport != ExportOrInheritedExport.None;
        }

        private bool TryHandleImportAttribute(CustomAttribute customAttribute, Handle attributeTypeHandle)
        {
            bool isImport = IsImportAttribute(attributeTypeHandle);
            if (!isImport)
            {
                return false;
            }

            var parent = customAttribute.Parent;
            switch (parent.HandleType)
            {
                case HandleType.Property:
                    MemberInfo propertyInfo = GetOrAddPropertyInfo((PropertyHandle)parent);
                    this.AddImportedMember(propertyInfo);
                    break;
                case HandleType.Field:
                    MemberInfo fieldInfo = GetOrAddFieldInfo((FieldHandle)parent);
                    this.AddImportedMember(fieldInfo);
                    break;
                case HandleType.Parameter:
                    // ignore Import attributes on parameters of importing constructors
                    break;
                case HandleType.Method:
                case HandleType.MethodImplementation:
                case HandleType.MethodSpecification:
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private bool TryHandleExportAttribute(CustomAttribute customAttribute, Handle attributeTypeHandle)
        {
            bool isInheritedExport = false;
            bool isExport = IsExportAttribute(attributeTypeHandle, out isInheritedExport);
            if (!isExport)
            {
                return false;
            }

            var parent = customAttribute.Parent;
            switch (parent.HandleType)
            {
                case HandleType.Type:
                    var typeHandle = (TypeHandle)parent;
                    TypeInfo exportedTypeInfo = GetOrCreateTypeInfo(typeHandle);
                    if (isInheritedExport)
                    {
                        //this.inheritedExportTypes.Add(typeHandle, true);
                        //this.inheritedExportTypesByName.Add(exportedTypeInfo.FullName);
                        //this.hasInheritedExports = true;
                    }
                    else
                    {
                        exportedTypeInfo.IsExported = true;
                    }

                    break;
                case HandleType.Property:
                    MemberInfo propertyInfo = GetOrAddPropertyInfo((PropertyHandle)parent);
                    this.AddExportedMember(propertyInfo);
                    break;
                case HandleType.Method:
                    MemberInfo methodInfo = GetOrAddMethodInfo((MethodHandle)parent);
                    this.AddExportedMember(methodInfo);
                    break;
                case HandleType.Field:
                    MemberInfo exportedFieldInfo = GetOrAddFieldInfo((FieldHandle)parent);
                    this.AddExportedMember(exportedFieldInfo);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private TypeInfo GetOrCreateTypeInfo(TypeHandle typeHandle)
        {
            TypeInfo typeInfo = null;
            if (this.compositionTypes.TryGetValue(typeHandle, out typeInfo))
            {
                return typeInfo;
            }

            typeInfo = new TypeInfo(isExported: false);
            typeInfo.MetadataToken = MetadataTokens.GetToken(metadataReader, typeHandle);

            this.compositionTypes.Add(typeHandle, typeInfo);
            return typeInfo;
        }

        private MemberInfo GetOrAddPropertyInfo(PropertyHandle handle)
        {
            if (this.properties == null)
            {
                properties = new Dictionary<PropertyHandle, MemberInfo>();
            }

            MemberInfo result = null;
            if (!properties.TryGetValue(handle, out result))
            {
                var property = metadataReader.GetProperty(handle);
                var propertyMethodHandles = property.GetAssociatedMethods();
                TypeHandle declaringTypeHandle;
                MethodHandle accessorMethod = propertyMethodHandles.Getter;
                if (accessorMethod.IsNil)
                {
                    accessorMethod = propertyMethodHandles.Setter;
                }

                declaringTypeHandle = metadataReader.GetDeclaringType(accessorMethod);
                result = new MemberInfo(MemberKind.Property);
                result.Token = this.metadataReader.GetToken(handle);
                result.DeclaringTypeHandle = declaringTypeHandle;
                result.Handle = handle;
                properties.Add(handle, result);
            }

            return result;
        }

        private MemberInfo GetOrAddFieldInfo(FieldHandle handle)
        {
            if (this.fields == null)
            {
                this.fields = new Dictionary<FieldHandle, MemberInfo>();
            }

            MemberInfo result = null;
            if (!fields.TryGetValue(handle, out result))
            {
                result = new MemberInfo(MemberKind.Field);
                var field = metadataReader.GetField(handle);
                result.Token = metadataReader.GetToken(handle);
                result.DeclaringTypeHandle = metadataReader.GetDeclaringType(handle);
                if (result.DeclaringTypeHandle.IsNil)
                {
                    throw null;
                }

                result.Handle = handle;
                fields[handle] = result;
            }

            return result;
        }

        private MemberInfo GetOrAddMethodInfo(MethodHandle handle)
        {
            if (this.methods == null)
            {
                this.methods = new Dictionary<MethodHandle, MemberInfo>();
            }

            MemberInfo result = null;
            if (!methods.TryGetValue(handle, out result))
            {
                var method = metadataReader.GetMethod(handle);
                result = new MemberInfo(MemberKind.Method);
                result.Token = this.metadataReader.GetToken(handle);
                result.DeclaringTypeHandle = metadataReader.GetDeclaringType(handle);
                result.Handle = handle;
                methods[handle] = result;
            }

            return result;
        }

        private void AddImportedMember(MemberInfo memberInfo)
        {
            var type = GetOrCreateTypeInfo(memberInfo.DeclaringTypeHandle);
            type.AddImportedMember(memberInfo);
        }

        private void AddExportedMember(MemberInfo memberInfo)
        {
            var type = GetOrCreateTypeInfo(memberInfo.DeclaringTypeHandle);
            type.AddExportedMember(memberInfo);
        }

        public ExportOrInheritedExport IsExportOrInheritedExportAttribute(Handle attributeTypeHandle)
        {
            var typeFullName = metadataReader.GetFullTypeName(attributeTypeHandle);
            if (knownExportAttributeOrDerivedType.Contains(typeFullName))
            {
                return ExportOrInheritedExport.Export;
            }

            return ExportOrInheritedExport.None;
        }

        public bool IsImportOrImportManyAttribute(Handle attributeTypeHandle)
        {
            var typeFullName = metadataReader.GetFullTypeName(attributeTypeHandle);
            if (knownImportAttributeOrDerivedType.Contains(typeFullName))
            {
                return true;
            }

            return false;
        }

        public enum ExportOrInheritedExport : byte
        {
            None,
            Export,
            InheritedExport
        }
    }
}
