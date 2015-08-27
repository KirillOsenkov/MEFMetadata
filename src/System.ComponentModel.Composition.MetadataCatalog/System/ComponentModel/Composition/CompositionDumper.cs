using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace System.ComponentModel.Composition
{
    /// <summary>
    /// Dumps parts, exports and imports to a text file.
    /// Useful to diff the results of two compositions to verify
    /// that different ways to compose produce same results.
    /// </summary>
    public sealed class CompositionDumper
    {
        private TextWriter writer;

        private CompositionDumper(TextWriter writer)
        {
            this.writer = writer;
        }

        public static void WriteTo(CompositionContainer compositionContainer, TextWriter writer)
        {
            var dumper = new CompositionDumper(writer);
            dumper.BuildCatalog(compositionContainer.Catalog);
        }

        public static void WriteTo(CompositionContainer compositionContainer, string fileName)
        {
            using (var sw = new StreamWriter(fileName))
            {
                WriteTo(compositionContainer, sw);
            }
        }

        public static async void TouchContainer(CompositionContainer container)
        {
            await TouchCatalog(container.Catalog).ConfigureAwait(false);
        }

        public static async Task TouchCatalog(ComposablePartCatalog composablePartCatalog)
        {
            var aggregate = composablePartCatalog as AggregateCatalog;
            if (aggregate != null)
            {
                //foreach (var catalog in aggregate.Catalogs)
                //{
                //    TouchCatalog(catalog);
                //}

                var actionList = new List<Task>();
                foreach (var item in aggregate.Catalogs)
                {
                    actionList.Add(TouchCatalog(item));
                }

                await Task.WhenAll(actionList.ToArray());
            }
            else
            {
                var metadataAssemblyCatalog = composablePartCatalog as MetadataAssemblyCatalog;
                if (metadataAssemblyCatalog != null)
                {
                    await metadataAssemblyCatalog.Realize();
                }

                foreach (var part in composablePartCatalog.Parts)
                {
                    TouchPart(part);
                }
            }
        }

        public static void TouchPart(ComposablePartDefinition part)
        {
            foreach (var export in part.ExportDefinitions)
            {
            }

            foreach (var import in part.ImportDefinitions)
            {
            }
        }

        private void BuildCatalog(ComposablePartCatalog composablePartCatalog)
        {
            var aggregateCatalog = composablePartCatalog as AggregateCatalog;
            if (aggregateCatalog != null)
            {
                foreach (var catalog in aggregateCatalog.Catalogs)
                {
                    BuildCatalog(catalog);
                }
            }
            else
            {
                foreach (var part in composablePartCatalog.Parts.OrderBy(p => p.ToString()))
                {
                    DumpPart(part);
                }
            }
        }

        public static string TimeComposition(IEnumerable<ComposablePartCatalog> catalogs, string outputFilePath = null)
        {
            var sw = Stopwatch.StartNew();

            var aggregateCatalog = new AggregateCatalog();

            foreach (var catalog in catalogs)
            {
                aggregateCatalog.Catalogs.Add(catalog);
            }

            var container = new CompositionContainer(
                aggregateCatalog,
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            var batch = new CompositionBatch();
            container.Compose(batch);

            TouchContainer(container);

            var elapsed = sw.Elapsed.ToString(@"s\.fff") + Environment.NewLine;

            if (outputFilePath != null)
            {
                WriteTo(container, outputFilePath);
            }

            return elapsed;
        }

        private void DumpPart(ComposablePartDefinition part)
        {
            Dump(part.ToString());

            foreach (var e in part.ExportDefinitions
                .OrderBy(export => export.ToString()))
            {
                DumpExport(part, e);
            }

            foreach (var i in part.ImportDefinitions
                .OrderBy(import => import.ToString()))
            {
                DumpImport(part, i);
            }
        }

        private void DumpImport(ComposablePartDefinition part, ImportDefinition i)
        {
            Dump("  Import: " + i.ToString());
        }

        private void DumpExport(ComposablePartDefinition part, ExportDefinition e)
        {
            Dump("  Export: " + e.ToString());
        }

        private void Dump(string p)
        {
            writer.WriteLine(p);
        }
    }
}
