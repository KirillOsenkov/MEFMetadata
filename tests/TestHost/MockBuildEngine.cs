using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace TestHost
{
    public class BuildEngine : IBuildEngine
    {
        public int ColumnNumberOfTaskNode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool ContinueOnError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineNumberOfTaskNode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ProjectFileOfTaskNode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
