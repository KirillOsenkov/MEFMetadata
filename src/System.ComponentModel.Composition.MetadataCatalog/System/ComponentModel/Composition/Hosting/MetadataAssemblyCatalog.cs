using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Composition.Metadata;

namespace System.ComponentModel.Composition.Hosting
{
    public class MetadataAssemblyCatalog : ComposablePartCatalog, ICompositionElement
    {
        private readonly object _thisLock = new object();
        private readonly ICompositionElement _definitionOrigin;
        private volatile Assembly _assembly = null;
        private volatile MetadataTypeCatalog _innerCatalog = null;
        private int _isDisposed = 0;
        private string _codeBase;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MetadataAssemblyCatalog"/> class
        ///     with the specified code base.
        /// </summary>
        /// <param name="codeBase">
        ///     A <see cref="String"/> containing the code base of the assembly containing the
        ///     attributed <see cref="Type"/> objects to add to the <see cref="MetadataAssemblyCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="codeBase"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="codeBase"/> is a zero-length string, contains only white space,
        ///     or contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     The caller does not have path discovery permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="codeBase"/> is not found.
        /// </exception>
        /// <exception cref="FileLoadException ">
        ///     <paramref name="codeBase"/> could not be loaded.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="codeBase"/> specified a directory.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        ///     <paramref name="codeBase"/> is not a valid assembly
        ///     -or-
        ///     Version 2.0 or later of the common language runtime is currently loaded
        ///     and <paramref name="codeBase"/> was compiled with a later version.
        /// </exception>
        /// <remarks>
        ///     The assembly referenced by <paramref langword="codeBase"/> is loaded into the Load context.
        /// </remarks>
        public MetadataAssemblyCatalog(string codeBase)
            : this(codeBase, (ICompositionElement)null)
        {
        }

        internal MetadataAssemblyCatalog(string codeBase, ICompositionElement definitionOrigin)
        {
            this._codeBase = codeBase;
            this._definitionOrigin = definitionOrigin ?? this;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MetadataAssemblyCatalog"/> class
        ///     with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects to
        ///     add to the <see cref="MetadataAssemblyCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="assembly"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="assembly"/> was loaded in the reflection-only context.
        /// </exception>
        public MetadataAssemblyCatalog(Assembly assembly)
            : this(assembly, (ICompositionElement)null)
        {
        }

        internal MetadataAssemblyCatalog(Assembly assembly, ICompositionElement definitionOrigin)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            this._assembly = assembly;
            this._definitionOrigin = definitionOrigin ?? this;
            this._codeBase = assembly.Location;
        }

        /// <summary>
        ///     Gets the assembly containing the attributed types contained within the assembly
        ///     catalog.
        /// </summary>
        /// <value>
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects
        ///     contained within the <see cref="MetadataAssemblyCatalog"/>.
        /// </value>
        public Assembly Assembly
        {
            get
            {
                if (this._assembly == null)
                {
                    this._assembly = AssemblyLoader.LoadAssembly(this._codeBase);
                }

                return _assembly;
            }
        }

        /// <summary>
        ///     Gets the part definitions of the assembly catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="IQueryable{T}"/> of <see cref="ComposablePartDefinition"/> objects of the
        ///     <see cref="MetadataAssemblyCatalog"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="MetadataAssemblyCatalog"/> has been disposed of.
        /// </exception>
        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                return this.InnerCatalog.Parts;
            }
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
            return this.InnerCatalog.GetExports(definition);
        }

        public Task Realize()
        {
            return InnerCatalog.Realize();
        }

        private MetadataTypeCatalog InnerCatalog
        {
            get
            {
                this.ThrowIfDisposed();

                if (this._innerCatalog == null)
                {
                    lock (this._thisLock)
                    {
                        if (this._innerCatalog == null)
                        {
                            var catalog = CreateTypeCatalog();
                            this._innerCatalog = catalog;
                        }
                    }
                }

                return this._innerCatalog;
            }
        }

        private MetadataTypeCatalog CreateTypeCatalog()
        {
            if (this._assembly == null)
            {
                return MetadataTypeCatalog.Create(this._codeBase, _definitionOrigin);
            }
            else
            {
                return MetadataTypeCatalog.Create(this.Assembly, _definitionOrigin);
            }
        }

        /// <summary>
        ///     Gets the display name of the assembly catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing a human-readable display name of the <see cref="MetadataAssemblyCatalog"/>.
        /// </value>
        string ICompositionElement.DisplayName
        {
            get { return this.GetDisplayName(); }
        }

        /// <summary>
        ///     Gets the composition element from which the assembly catalog originated.
        /// </summary>
        /// <value>
        ///     This property always returns <see langword="null"/>.
        /// </value>
        ICompositionElement ICompositionElement.Origin
        {
            get { return null; }
        }

        /// <summary>
        ///     Returns a string representation of the assembly catalog.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the string representation of the <see cref="MetadataAssemblyCatalog"/>.
        /// </returns>
        public override string ToString()
        {
            return this.GetDisplayName();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Interlocked.CompareExchange(ref this._isDisposed, 1, 0) == 0)
                {
                    if (disposing)
                    {
                        if (this._innerCatalog != null)
                        {
                            this._innerCatalog.Dispose();
                        }
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ThrowIfDisposed()
        {
            if (this._isDisposed == 1)
            {
                throw new ObjectDisposedException("MetadataAssemblyCatalog");
            }
        }

        private string GetDisplayName()
        {
            return string.Format(CultureInfo.CurrentCulture,
                                "{0} (Assembly=\"{1}\")",   // NOLOC
                                GetType().Name,
                                this._codeBase ?? this.Assembly.FullName);
        }
    }
}
