using System.Reflection.Metadata;

namespace Microsoft.Composition.Metadata
{
    public class MemberInfo
    {
        public MemberInfo(MemberKind kind)
        {
            this.Kind = kind;
        }

        public TypeHandle DeclaringTypeHandle { get; set; }
        public Handle Handle { get; internal set; }
        public int Token { get; internal set; }
        public TypeInfo Type { get; internal set; }
        public MemberKind Kind { get; set; }
    }
}
