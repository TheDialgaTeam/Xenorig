using System.Threading;
using Xirorig.Options.Xirorig;

namespace Xirorig.Options
{
    internal class XirorigOptions
    {
        public int PrintTime { get; set; }

        public PeerNode[] PeerNodes { get; set; }

        public int NumberOfThreads { get; set; }

        public ThreadPriority ThreadPriority { get; set; }
    }
}