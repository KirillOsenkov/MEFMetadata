using System.ComponentModel.Composition;

namespace TestAssemblyA
{
    [Export]
    public class TestExportedType
    {
        [Import]
        private string importField = null;

        [Import]
        public string ImportProperty { get; set; }

        [Export]
        public string ExportProperty
        {
            get
            {
                return null;
            }
        }

        [Export]
        public string ExportMethod()
        {
            return null;
        }

        [Export]
        private string exportField = null;
    }

    public class DerivedExportAttribute : ExportAttribute
    {
    }
}
