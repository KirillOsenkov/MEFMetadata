using System.Runtime.CompilerServices;
using TestAssemblyA;

[assembly: InternalsVisibleTo("TestAssemblyC")]

namespace TestAssemblyB
{
    [TestAssemblyA.DerivedExport]
    public class ClassMarkedWithDerivedExportAttribute
    {
    }

    public class MoreDerivedExportAttribute : DerivedExportAttribute
    {
    }
}
