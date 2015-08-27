using System;
using System.Diagnostics;

namespace TestHost
{
    public class DisposableTiming : IDisposable
    {
        private string message;
        private Stopwatch sw;

        public DisposableTiming(string message)
        {
            this.sw = Stopwatch.StartNew();
            this.message = message;
        }

        public void Dispose()
        {
            Console.WriteLine(message + ": " + sw.Elapsed.ToString("s\\.fff"));
            if (message == "Everything")
            {
                Program.Log(sw.Elapsed.ToString("s\\.fff"));
            }
        }
    }
}
