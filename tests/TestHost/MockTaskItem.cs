﻿using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace TestHost
{
    public class TaskItem : ITaskItem
    {
        public TaskItem(string itemSpec)
        {
            this.ItemSpec = itemSpec;
        }

        public string ItemSpec { get; set; }

        public int MetadataCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection MetadataNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IDictionary CloneCustomMetadata()
        {
            throw new NotImplementedException();
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            throw new NotImplementedException();
        }

        public string GetMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void RemoveMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
            throw new NotImplementedException();
        }
    }
}
