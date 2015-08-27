using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Microsoft.Composition.Metadata
{
    public static class ExtensionMethods
    {
        public static string GetFullTypeName(this MetadataReader metadataReader, Handle handle)
        {
            if (handle.HandleType == HandleType.Type)
            {
                return GetFullTypeName(metadataReader, (TypeHandle)handle);
            }
            else if (handle.HandleType == HandleType.TypeReference)
            {
                return GetFullTypeName(metadataReader, (TypeReferenceHandle)handle);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static string GetFullTypeName(this MetadataReader metadataReader, TypeReferenceHandle typeReferenceHandle)
        {
            var typeReference = metadataReader.GetTypeReference(typeReferenceHandle);
            return GetFullTypeName(metadataReader, typeReference);
        }

        public static string GetFullTypeName(this MetadataReader metadataReader, TypeHandle typeHandle)
        {
            var typeDefinition = metadataReader.GetTypeDefinition(typeHandle);
            return metadataReader.GetFullTypeName(typeDefinition);
        }

        public static string GetFullTypeName(this MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            var name = metadataReader.GetString(typeDefinition.Namespace) ?? "";
            if (name != "")
            {
                name = name + ".";
            }

            name = name + metadataReader.GetString(typeDefinition.Name);
            return name;
        }

        public static string GetFullTypeName(this MetadataReader metadataReader, TypeReference typeReference)
        {
            var name = metadataReader.GetString(typeReference.Namespace) ?? "";
            if (name != "")
            {
                name = name + ".";
            }

            name = name + metadataReader.GetString(typeReference.Name);
            return name;
        }

        public static string GetFullAssemblyName(this MetadataReader metadataReader)
        {
            return GetFullAssemblyName(metadataReader, metadataReader.GetAssemblyDefinition());
        }

        public static string GetFullAssemblyName(this MetadataReader metadataReader, AssemblyDefinition assemblyDefinition)
        {
            var assemblyName = metadataReader.GetString(assemblyDefinition.Name);
            var culture = metadataReader.GetString(assemblyDefinition.Culture);
            if (string.IsNullOrEmpty(culture))
            {
                culture = "neutral";
            }

            var version = assemblyDefinition.Version;
            var publicKeyBytes = metadataReader.GetBytes(assemblyDefinition.PublicKey);
            byte[] publicKeyTokenBytes = publicKeyBytes;
            string publicKeyToken = "null";
            if (publicKeyBytes.Length > 0)
            {
                publicKeyTokenBytes = CalculatePublicKeyToken(publicKeyBytes);
                publicKeyToken = Conversion.ByteArrayToHexString(publicKeyTokenBytes);
            }

            var fullAssemblyName = string.Format(
                "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                assemblyName,
                version.ToString(),
                culture,
                publicKeyToken);
            return fullAssemblyName;
        }

        public static string GetFullAssemblyName(this MetadataReader metadataReader, AssemblyReference assemblyReference)
        {
            var assemblyName = metadataReader.GetString(assemblyReference.Name);
            var culture = metadataReader.GetString(assemblyReference.Culture);
            if (string.IsNullOrEmpty(culture))
            {
                culture = "neutral";
            }

            var version = assemblyReference.Version;
            var publicKeyTokenBytes = metadataReader.GetBytes(assemblyReference.PublicKeyOrToken);
            var publicKeyToken = Conversion.ByteArrayToHexString(publicKeyTokenBytes);
            publicKeyToken = string.IsNullOrEmpty(publicKeyToken) ? "null" : publicKeyToken;

            var fullAssemblyName = string.Format(
                "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                assemblyName,
                version.ToString(),
                culture,
                publicKeyToken);
            return fullAssemblyName;
        }

        public static IEnumerable<string> GetReferenceAssemblyPartialNames(this MetadataReader metadataReader)
        {
            foreach (var assemblyReferenceHandle in metadataReader.AssemblyReferences)
            {
                var assemblyReference = metadataReader.GetAssemblyReference(assemblyReferenceHandle);
                var partialName = metadataReader.GetString(assemblyReference.Name);
                yield return partialName;
            }
        }

        public static bool ReferencesAssembly(this MetadataReader metadataReader, string partialAssemblyName)
        {
            var references = metadataReader.GetReferenceAssemblyPartialNames();
            return references.Any(r => string.Equals(r, partialAssemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<string> GetReferenceAssemblyFullNames(this MetadataReader metadataReader)
        {
            foreach (var assemblyReferenceHandle in metadataReader.AssemblyReferences)
            {
                var assemblyReference = metadataReader.GetAssemblyReference(assemblyReferenceHandle);
                var fullAssemblyName = metadataReader.GetFullAssemblyName(assemblyReference);
                yield return fullAssemblyName;
            }
        }

        private static byte[] CalculatePublicKeyToken(byte[] publicKeyBytes)
        {
            using (var sha1 = new SHA1Cng())
            {
                publicKeyBytes = sha1.ComputeHash(publicKeyBytes);
            }

            byte[] result = new byte[8];
            int length = publicKeyBytes.Length - 1;
            for (int i = 0; i < 8; i++)
            {
                result[i] = publicKeyBytes[length - i];
            }

            return result;
        }

        public static Handle GetAttributeTypeHandle(this MetadataReader metadataReader, CustomAttribute customAttribute)
        {
            Handle attributeTypeHandle;
            var ctor = customAttribute.Constructor;
            if (ctor.HandleType == HandleType.MemberReference)
            {
                var memberReferenceHandle = (MemberReferenceHandle)ctor;
                var memberReference = metadataReader.GetMemberReference(memberReferenceHandle);
                attributeTypeHandle = memberReference.Parent;
            }
            else
            {
                MethodHandle methodHandle = (MethodHandle)ctor;
                var method = metadataReader.GetMethod(methodHandle);
                attributeTypeHandle = metadataReader.GetDeclaringType(methodHandle);
            }

            return attributeTypeHandle;
        }
    }
}
