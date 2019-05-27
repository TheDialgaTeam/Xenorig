
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
#if WIN
using System;

namespace TheDialgaTeam.Xiropht.Xirorig.Core
{

    public class DistributedThread
    {
        private readonly ThreadStart threadStart;

        private readonly ParameterizedThreadStart parameterizedThreadStart;

        public int ProcessorAffinity { get; set; }

        public Thread ManagedThread { get; }

        private ProcessThread CurrentThread
        {
            get
            {
                var id = GetCurrentThreadId();
                return (from ProcessThread th in Process.GetCurrentProcess().Threads where th.Id == id select th).Single();
            }
        }

        public DistributedThread(ThreadStart threadStart) : this()
        {
            this.threadStart = threadStart;
        }

        public DistributedThread(ParameterizedThreadStart threadStart) : this()
        {
            parameterizedThreadStart = threadStart;
        }

        private DistributedThread()
        {
            ManagedThread = new Thread(DistributedThreadStart);
        }

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentProcessorNumber();

        public void Start()
        {
            if (threadStart == null)
                throw new InvalidOperationException();

            ManagedThread.Start(null);
        }

        public void Start(object parameter)
        {
            if (parameterizedThreadStart == null)
                throw new InvalidOperationException();

            ManagedThread.Start(parameter);
        }

        private void DistributedThreadStart(object parameter)
        {
            try
            {
                Thread.BeginThreadAffinity();

                if (ProcessorAffinity != 0)
                    CurrentThread.ProcessorAffinity = new IntPtr(ProcessorAffinity);

                if (threadStart != null)
                    threadStart();
                else if (parameterizedThreadStart != null)
                    parameterizedThreadStart(parameter);
                else
                    throw new InvalidOperationException();
            }
            finally
            {
                CurrentThread.ProcessorAffinity = new IntPtr(0xFFFF);
                Thread.EndThreadAffinity();
            }
        }
    }
}
#endif