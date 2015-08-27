// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     An immutable ComposablePartCatalog created from a type array or a list of managed types.  This class is threadsafe.
    ///     It is Disposable.
    /// </summary>
    public partial class MetadataTypeCatalog : ComposablePartCatalog, ICompositionElement
    {
        private readonly object _thisLock = new object();
        private IEnumerable<Type> _types = null;
        private volatile IQueryable<ComposablePartDefinition> _queryableParts;
        private volatile bool _isDisposed = false;
        private readonly ICompositionElement _definitionOrigin;
        private readonly Lazy<IDictionary<string, List<ComposablePartDefinition>>> _contractPartIndex;

        public static IMetadataInfo GetOrCreateMetadataInfo(Assembly assembly)
        {
            return MetadataInfo.GetOrCreate(assembly);
        }

        public static IMetadataInfo GetOrCreateMetadataInfoByAssemblyFullName(string assemblyFullName)
        {
            return MetadataInfo.GetOrCreateByAssemblyFullName(assemblyFullName);
        }

        public static MetadataTypeCatalog Create(Assembly assembly, ICompositionElement definitionOrigin)
        {
            var metadataInfo = GetOrCreateMetadataInfo(assembly);
            return new MetadataTypeCatalog(metadataInfo, definitionOrigin);
        }

        public static MetadataTypeCatalog Create(string assemblyFilePath, ICompositionElement definitionOrigin)
        {
            var metadataInfo = MetadataInfo.GetOrCreate(assemblyFilePath);
            return new MetadataTypeCatalog(metadataInfo, definitionOrigin);
        }

        public Task Realize()
        {
            return metadataInfo.Realize();
        }

        /// <summary>
        ///     Gets the part definitions of the catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="IQueryable{T}"/> of <see cref="ComposablePartDefinition"/> objects of the 
        ///     <see cref="MetadataTypeCatalog"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="MetadataTypeCatalog"/> has been disposed of.
        /// </exception>
        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                this.ThrowIfDisposed();

                return this.PartsInternal;
            }
        }

        /// <summary>
        ///     Gets the display name of the type catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing a human-readable display name of the <see cref="MetadataTypeCatalog"/>.
        /// </value>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        string ICompositionElement.DisplayName
        {
            get { return this.GetDisplayName(); }
        }

        /// <summary>
        ///     Gets the composition element from which the type catalog originated.
        /// </summary>
        /// <value>
        ///     This property always returns <see langword="null"/>.
        /// </value>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        ICompositionElement ICompositionElement.Origin
        {
            get { return null; }
        }

        /// <summary>
        ///     Returns the export definitions that match the constraint defined by the specified definition.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the 
        ///     <see cref="ExportDefinition"/> objects to return.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Tuple{T1, T2}"/> containing the 
        ///     <see cref="ExportDefinition"/> objects and their associated 
        ///     <see cref="ComposablePartDefinition"/> for objects that match the constraint defined 
        ///     by <paramref name="definition"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ComposablePartCatalog"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/>, if no 
        ///         <see cref="ExportDefinition"/> match the conditions defined by 
        ///         <paramref name="definition"/>, return an empty <see cref="IEnumerable{T}"/>.
        ///     </note>
        /// </remarks>
        public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
        {
            this.ThrowIfDisposed();

            IEnumerable<ComposablePartDefinition> candidateParts = this.GetCandidateParts(definition);
            if (candidateParts == null)
            {
                return Enumerable.Empty<Tuple<ComposablePartDefinition, ExportDefinition>>();
            }

            var exports = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();
            foreach (var part in candidateParts)
            {
                foreach (var export in part.ExportDefinitions)
                {
                    if (definition.IsConstraintSatisfiedBy(export))
                    {
                        exports.Add(new Tuple<ComposablePartDefinition, ExportDefinition>(part, export));
                    }
                }
            }

            return exports;
        }

        private IEnumerable<ComposablePartDefinition> GetCandidateParts(ImportDefinition definition)
        {
            string contractName = definition.ContractName;

            // Empty string represents a non-contract based import and thus the constraint needs
            // to be applied to all the possible exports in this catalog.
            if (string.IsNullOrEmpty(contractName))
            {
                return this.PartsInternal;
            }

            List<ComposablePartDefinition> candidateParts = null;
            if (this._contractPartIndex.Value.TryGetValue(contractName, out candidateParts))
            {
                return candidateParts;
            }
            else
            {
                return null;
            }
        }

        private IDictionary<string, List<ComposablePartDefinition>> CreateIndex()
        {
            Dictionary<string, List<ComposablePartDefinition>> index = new Dictionary<string, List<ComposablePartDefinition>>(StringComparer.Ordinal);

            foreach (var part in this.PartsInternal)
            {
                foreach (string contractName in part.ExportDefinitions.Select(export => export.ContractName).Distinct())
                {
                    List<ComposablePartDefinition> contractParts = null;
                    if (!index.TryGetValue(contractName, out contractParts))
                    {
                        contractParts = new List<ComposablePartDefinition>();
                        index.Add(contractName, contractParts);
                    }

                    contractParts.Add(part);
                }
            }

            return index;
        }

        /// <summary>
        ///     Returns a string representation of the type catalog.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the string representation of the <see cref="MetadataTypeCatalog"/>.
        /// </returns>
        public override string ToString()
        {
            return this.GetDisplayName();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._isDisposed = true;
            }

            base.Dispose(disposing);
        }

        private string GetDisplayName()
        {
            return String.Format(CultureInfo.CurrentCulture,
                                "TypeCatalog_DisplayNameFormat",
                                this.GetType().Name,
                                this.GetTypesDisplay());
        }

        private string GetTypesDisplay()
        {
            int count = this.PartsInternal.Count();
            if (count == 0)
            {
                return "TypeCatalog_Empty";
            }

            const int displayCount = 2;
            StringBuilder builder = new StringBuilder();
            foreach (ComposablePartDefinition definition in this.PartsInternal.Take(displayCount))
            {
                if (builder.Length > 0)
                {
                    builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    builder.Append(" ");
                }

                builder.Append(definition.ToString());
            }

            if (count > displayCount)
            {   // Add an elipse to indicate that there 
                // are more types than actually listed
                builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                builder.Append(" ...");
            }

            return builder.ToString();
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (this._isDisposed)
            {
                throw new ObjectDisposedException("MetadataTypeCatalog");
            }
        }
    }
}
