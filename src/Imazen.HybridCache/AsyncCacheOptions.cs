using System;

namespace Imazen.HybridCache
{
    public class AsyncCacheOptions
    {
        public int WaitForIdenticalRequestsTimeoutMs { get; set; } = 100000;
        public int WaitForIdenticalDiskWritesMs { get; set;  } = 15000;
        public long MaxQueuedBytes { get; set;  } = 1024 * 1024 * 100;

        public bool FailRequestsOnEnqueueLockTimeout { get; set; } = true;
        
        public bool WriteSynchronouslyWhenQueueFull { get; set; } = true;
        
        /// <summary>
        /// If this is used from .NET Core, set to File.Move(from, to, true)
        /// Used by deletion code even if MoveFilesIntoPlace is false
        /// </summary>
        public Action<string,string> MoveFileOverwriteFunc { get; set; }

        /// <summary>
        /// If true, cache files are first written to a temp file, then moved into their correct place.
        /// Slightly slower when true. Defaults to false.
        /// </summary>
        public bool MoveFilesIntoPlace { get; set; }
    }
}