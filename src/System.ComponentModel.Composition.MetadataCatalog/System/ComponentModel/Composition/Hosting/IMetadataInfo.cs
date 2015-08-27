using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace System.ComponentModel.Composition.Hosting
{
    public interface IMetadataInfo
    {
        IEnumerable<Type> ExportedTypes { get; }
        IEnumerable<MemberInfo> GetExportedMembers(Type type);
        IEnumerable<MemberInfo> GetImportedMembers(Type type);
        string AssemblyFullName { get; }

        /// <summary>
        /// Triggers realization of the lazy inner catalog on a background thread to "warm up".
        /// Hopefully by the time the content will be requested, it will already be there.
        /// Calling this method is not required and just serves to preheat the cache.
        /// </summary>
        /// <returns>A task that completes once the catalog contents has been realized.</returns>
        Task Realize();
    }
}
