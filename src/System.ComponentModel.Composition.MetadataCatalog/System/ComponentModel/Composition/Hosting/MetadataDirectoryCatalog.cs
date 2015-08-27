using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Composition.Metadata;

namespace System.ComponentModel.Composition.Hosting
{
    public class MetadataDirectoryCatalog : ComposablePartCatalog, ICompositionElement
    {
        private readonly string filePath;
        private readonly string searchPattern = "*.dll";
        private AggregateCatalog aggregateCatalog;
        private IQueryable<ComposablePartDefinition> parts;

        public MetadataDirectoryCatalog(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Environment.CurrentDirectory;
            }

            if (!Directory.Exists(filePath))
            {
                throw new DirectoryNotFoundException("Directory not found: " + filePath);
            }

            this.filePath = filePath;
        }

        public MetadataDirectoryCatalog(string filePath, string searchPattern)
            : this(filePath)
        {
            this.searchPattern = searchPattern;
        }

        public string FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        public string SearchPattern
        {
            get
            {
                return this.searchPattern;
            }
        }

        public string DisplayName
        {
            get
            {
                return string.Format("MetadataDirectoryCatalog ({0})", FilePath);
            }
        }

        public ICompositionElement Origin
        {
            get
            {
                return null;
            }
        }

        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                if (this.parts == null)
                {
                    Initialize();
                }

                return this.parts;
            }
        }

        protected virtual void Initialize()
        {
            aggregateCatalog = new AggregateCatalog();

            var files = Directory.GetFiles(FilePath, SearchPattern);
            foreach (var assemblyFile in files)
            {
                var catalog = new MetadataAssemblyCatalog(assemblyFile);
                aggregateCatalog.Catalogs.Add(catalog);
            }

            parts = aggregateCatalog.Parts;
        }
    }
}
