using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Composition.Metadata
{
    public class TypeInfo
    {
        public TypeInfo(bool isExported)
        {
            this.IsExported = isExported;
        }

        public int MetadataToken { get; internal set; }
        public bool IsExported { get; set; }

        private List<MemberInfo> exportedMembers = null;
        private List<MemberInfo> importedMembers = null;

        public IEnumerable<MemberInfo> ExportedMembers
        {
            get
            {
                if (exportedMembers == null)
                {
                    return Enumerable.Empty<MemberInfo>();
                }

                return exportedMembers;
            }
        }

        public bool HasImportedMembers
        {
            get
            {
                return importedMembers != null && importedMembers.Count > 0;
            }
        }

        public IEnumerable<MemberInfo> ImportedMembers
        {
            get
            {
                if (importedMembers == null)
                {
                    return Enumerable.Empty<MemberInfo>();
                }

                return importedMembers;
            }
        }

        public void AddExportedMember(MemberInfo memberInfo)
        {
            if (exportedMembers == null)
            {
                exportedMembers = new List<MemberInfo>();
            }

            foreach (var existingExportedMember in exportedMembers)
            {
                if (existingExportedMember.Handle == memberInfo.Handle)
                {
                    return;
                }
            }

            exportedMembers.Add(memberInfo);
            memberInfo.Type = this;
            this.IsExported = true;
        }

        public void AddImportedMember(MemberInfo memberInfo)
        {
            if (importedMembers == null)
            {
                importedMembers = new List<MemberInfo>();
            }

            foreach (var existingImportedMember in importedMembers)
            {
                if (existingImportedMember.Handle == memberInfo.Handle)
                {
                    return;
                }
            }

            importedMembers.Add(memberInfo);
            memberInfo.Type = this;
        }
    }
}
