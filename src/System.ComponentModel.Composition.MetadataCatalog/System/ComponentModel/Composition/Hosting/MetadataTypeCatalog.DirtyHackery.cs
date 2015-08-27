// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class MetadataTypeCatalog
    {
        private static Assembly mef;
        private static Type typeofObject;
        private static Type typeofInheritedExport;
        private static Type typeofReflectionComposablePartDefinition;
        private static Type compositionServices;
        private static ConstructorInfo attributedPartCreationInfoCtor;
        private static ConstructorInfo attributedExportDefinitionCtor;
        private static ConstructorInfo reflectionComposablePartDefinitionCtor;
        private static ConstructorInfo reflectionMemberExportDefinitionCtor;
        private static ConstructorInfo reflectionPropertyCtor;
        private static FieldInfo attributedPartCreationInfoExports;
        private static FieldInfo attributedPartCreationInfoImports;
        private static MethodInfo attributedPartCreationInfoGetConstructor;
        private static MethodInfo attributedPartCreationInfoCreateExportDefinition;
        private static MethodInfo attributedModelDiscoveryCreateParameterImportDefinition;
        private static MethodInfo attributedModelDiscoveryCreateMemberImportDefinition;
        private static MethodInfo getContractInfoFromExport;

        private static Func<MemberInfo, ICompositionElement, ImportDefinition>
                attributedModelDiscoveryCreateMemberImportDefinitionDelegate;
        private static Func<ParameterInfo, ICompositionElement, ImportDefinition>
                attributedModelDiscoveryCreateParameterImportDefinitionDelegate;

        private IMetadataInfo metadataInfo;

        static MetadataTypeCatalog()
        {
            mef = typeof(TypeCatalog).Assembly;
            var attributedPartCreationInfo = mef.GetType("System.ComponentModel.Composition.AttributedModel.AttributedPartCreationInfo");
            attributedPartCreationInfoCtor = attributedPartCreationInfo.GetConstructor(new[]
            {
                typeof(Type),
                typeof(PartCreationPolicyAttribute),
                typeof(bool),
                typeof(ICompositionElement)
            });
            attributedPartCreationInfoExports = attributedPartCreationInfo.GetField("_exports", BindingFlags.NonPublic | BindingFlags.Instance);
            attributedPartCreationInfoImports = attributedPartCreationInfo.GetField("_imports", BindingFlags.NonPublic | BindingFlags.Instance);
            attributedPartCreationInfoGetConstructor = attributedPartCreationInfo.GetMethod("GetConstructor");
            attributedPartCreationInfoCreateExportDefinition = attributedPartCreationInfo.GetMethod("CreateExportDefinition", BindingFlags.NonPublic | BindingFlags.Instance);

            var attributedModelDiscovery = mef.GetType("System.ComponentModel.Composition.AttributedModel.AttributedModelDiscovery");
            attributedModelDiscoveryCreateMemberImportDefinition = attributedModelDiscovery.GetMethod("CreateMemberImportDefinition");
            attributedModelDiscoveryCreateMemberImportDefinitionDelegate =
                (Func<MemberInfo, ICompositionElement, ImportDefinition>)
                Delegate.CreateDelegate(
                    typeof(Func<MemberInfo, ICompositionElement, ImportDefinition>),
                    attributedModelDiscoveryCreateMemberImportDefinition);

            attributedModelDiscoveryCreateParameterImportDefinition = attributedModelDiscovery.GetMethod("CreateParameterImportDefinition");
            attributedModelDiscoveryCreateParameterImportDefinitionDelegate =
                (Func<ParameterInfo, ICompositionElement, ImportDefinition>)
                Delegate.CreateDelegate(
                    typeof(Func<ParameterInfo, ICompositionElement, ImportDefinition>),
                    attributedModelDiscoveryCreateParameterImportDefinition);

            var attributedExportDefinition = mef.GetType("System.ComponentModel.Composition.AttributedModel.AttributedExportDefinition");
            attributedExportDefinitionCtor = attributedExportDefinition.GetConstructor(new[]
            {
                attributedPartCreationInfo,
                typeof(MemberInfo),
                typeof(ExportAttribute),
                typeof(Type),
                typeof(string)
            });

            compositionServices = mef.GetType("System.ComponentModel.Composition.Hosting.CompositionServices");
            getContractInfoFromExport = compositionServices.GetMethod(
                "GetContractInfoFromExport",
                BindingFlags.Static | BindingFlags.NonPublic);

            typeofReflectionComposablePartDefinition = mef.GetType("System.ComponentModel.Composition.ReflectionModel.ReflectionComposablePartDefinition");
            var iReflectionPartCreationInfo = mef.GetType("System.ComponentModel.Composition.ReflectionModel.IReflectionPartCreationInfo");
            reflectionComposablePartDefinitionCtor = typeofReflectionComposablePartDefinition.GetConstructor(new[]
            {
                iReflectionPartCreationInfo
            });

            var reflectionProperty = mef.GetType("System.ComponentModel.Composition.ReflectionModel.ReflectionProperty");
            reflectionPropertyCtor = reflectionProperty.GetConstructor(new[]
            {
                typeof(MethodInfo),
                typeof(MethodInfo)
            });

            var reflectionMemberExportDefinition = mef.GetType("System.ComponentModel.Composition.ReflectionModel.ReflectionMemberExportDefinition");
            reflectionMemberExportDefinitionCtor = reflectionMemberExportDefinition.GetConstructor(new[]
                {
                    typeof(LazyMemberInfo),
                    typeof(ExportDefinition),
                    typeof(ICompositionElement)
                });

            typeofInheritedExport = typeof(InheritedExportAttribute);
            typeofObject = typeof(object);
        }

        public MetadataTypeCatalog(IMetadataInfo metadataInfo, ICompositionElement definitionOrigin)
        {
            this.metadataInfo = metadataInfo;
            this._definitionOrigin = definitionOrigin ?? this;
            this._contractPartIndex = new Lazy<IDictionary<string, List<ComposablePartDefinition>>>(this.CreateIndex, true);
        }

        private IEnumerable<Type> Types
        {
            get
            {
                if (this._types == null)
                {
                    _types = this.metadataInfo.ExportedTypes;
                }

                return this._types;
            }
        }

        private IQueryable<ComposablePartDefinition> PartsInternal
        {
            get
            {
                if (this._queryableParts == null)
                {
                    lock (this._thisLock)
                    {
                        if (this._queryableParts == null)
                        {
                            var collection = new List<ComposablePartDefinition>();
#if SINGLETHREADED
                            foreach (Type type in this._types)
                            {
                                ComposablePartDefinition definition = GetComposablePartDefinition(type, _definitionOrigin);
                                if (definition != null)
                                {
                                    //lock (collection)
                                    {
                                        collection.Add(definition);
                                    }
                                }
                            }
#else
                            Parallel.ForEach(Types, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, type =>
                            {
                                ComposablePartDefinition definition = GetComposablePartDefinition(type, _definitionOrigin);
                                if (definition != null)
                                {
                                    lock (collection)
                                    {
                                        collection.Add(definition);
                                    }
                                }
                            });
#endif

                            IQueryable<ComposablePartDefinition> queryableParts = collection.AsQueryable();
                            Thread.MemoryBarrier();

                            this._queryableParts = queryableParts;
                        }
                    }
                }

                return this._queryableParts;
            }
        }

        public string AssemblyFullName
        {
            get
            {
                return this.metadataInfo.AssemblyFullName;
            }
        }

        private ComposablePartDefinition GetComposablePartDefinition(Type type, ICompositionElement origin)
        {
            ICompositionElement creationInfo = (ICompositionElement)
                attributedPartCreationInfoCtor.Invoke(new object[] { type, null, false, origin });

            // This is where we inject our own imports and exports into the AttributedPartCreationInfo
            // Fortunately for us they're lazy loaded in there, so we manage to squeeze our stuff in
            // before they're defaulted to their expensive initialization codepaths.
            HackCreationInfo(type, creationInfo);

            ComposablePartDefinition result = (ComposablePartDefinition)
                reflectionComposablePartDefinitionCtor.Invoke(new object[] { creationInfo });

            return result;
        }

        private void HackCreationInfo(Type type, ICompositionElement creationInfo)
        {
            SetExports(type, creationInfo);
            SetImports(type, creationInfo);
        }
    }
}
