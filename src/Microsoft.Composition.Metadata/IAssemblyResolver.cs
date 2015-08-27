namespace Microsoft.Composition.Metadata
{
    public interface IAssemblyResolver
    {
        string ResolveAssembly(string assemblyName);
    }
}
