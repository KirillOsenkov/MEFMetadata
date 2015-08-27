using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Composition.Metadata
{
    public class AssemblyLoader
    {
        private static readonly char[] commaArray = new char[] { ',' };
        private static readonly char[] spaceArray = new char[] { ' ' };

        public static Assembly LoadAssembly(string codeBase)
        {
            Debug.WriteLine("Assembly.Load: " + codeBase);

            AssemblyName assemblyName;

            try
            {
                assemblyName = AssemblyName.GetAssemblyName(codeBase);
            }
            catch (ArgumentException)
            {
                assemblyName = new AssemblyName();
                assemblyName.CodeBase = codeBase;
            }

            return Assembly.Load(assemblyName);
        }

        private static AssemblyName GetAssemblyName(string codeBase, string fullName)
        {
            var assemblyName = new AssemblyName();
            var parts = fullName.Split(commaArray);
            assemblyName.Name = parts[0].TrimEnd(spaceArray);
            assemblyName.Version = Version.Parse(GetValue(parts[1]));
            assemblyName.CultureInfo = GetCultureInfo(GetValue(parts[2]));
            var publicKeyTokenString = GetValue(parts[3]);
            if (publicKeyTokenString != "null")
            {
                assemblyName.SetPublicKeyToken(Conversion.HexStringToByteArray(publicKeyTokenString));
            }
            else
            {
                assemblyName.SetPublicKeyToken(new byte[0]);
            }

            assemblyName.CodeBase = "file:///" + codeBase.Replace('\\', '/');
            return assemblyName;
        }

        private static CultureInfo GetCultureInfo(string cultureString)
        {
            if (cultureString == "neutral")
            {
                return CultureInfo.InvariantCulture;
            }

            return CultureInfo.GetCultureInfo(cultureString);
        }

        private static string GetValue(string nameValuePair)
        {
            nameValuePair = nameValuePair.TrimEnd(spaceArray);
            int equalsIndex = nameValuePair.IndexOf('=');
            if (equalsIndex == -1)
            {
                return nameValuePair;
            }

            return nameValuePair.Substring(equalsIndex + 1);
        }

        public static Assembly LoadAssemblyByName(string assemblyName)
        {
            Debug.WriteLine("Assembly.Load: " + assemblyName);
            return Assembly.Load(assemblyName);
        }
    }
}
