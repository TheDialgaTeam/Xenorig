using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xirorig.Algorithm.Xiropht.Decentralized;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;

namespace Xirorig.Miner.Backend
{
    internal delegate void CpuMinerJobLog(string message, bool includeDefaultTemplate, params object[] args);

    internal delegate void CpuMinerJobResultFound(IJobTemplate jobTemplate, IJobResult jobResult);

    internal abstract class CpuMiner : IDisposable
    {
        public event CpuMinerJobLog? JobLog;
        public event CpuMinerJobResultFound? JobResultFound;

        private readonly CpuMinerThreadConfiguration _threadConfiguration;
        private readonly Thread _thread;
        private readonly BlockingCollection<IJobTemplate> _jobTemplateQueue = new();
        private readonly CancellationTokenSource _cancellationTokenSource;

        private CancellationTokenSource _jobCancellationTokenSource = new();

        private long _totalHashCalculatedIn10Seconds;
        private long _totalHashCalculatedIn60Seconds;
        private long _totalHashCalculatedIn15Minutes;

        public long TotalHashCalculatedIn10Seconds
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn10Seconds : Interlocked.Read(ref _totalHashCalculatedIn10Seconds);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn10Seconds = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds, value);
                }
            }
        }

        public long TotalHashCalculatedIn60Seconds
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn60Seconds : Interlocked.Read(ref _totalHashCalculatedIn60Seconds);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn60Seconds = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn60Seconds, value);
                }
            }
        }

        public long TotalHashCalculatedIn15Minutes
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn15Minutes : Interlocked.Read(ref _totalHashCalculatedIn15Minutes);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn15Minutes = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn15Minutes, value);
                }
            }
        }

        protected int ThreadId { get; }

        protected CpuMiner(int threadId, CpuMinerThreadConfiguration threadConfiguration, CancellationToken cancellationToken)
        {
            ThreadId = threadId;
            _threadConfiguration = threadConfiguration;
            _thread = new Thread(ConfigureMiningThread) { IsBackground = true, Priority = threadConfiguration.GetThreadPriority() };
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public static CpuMiner CreateCpuMiner(int threadId, CpuMinerThreadConfiguration threadConfiguration, CancellationToken cancellationToken, Pool pool)
        {
            var poolAlgorithm = pool.GetAlgorithm();

            if (poolAlgorithm.Equals("xiropht_decentralized", StringComparison.OrdinalIgnoreCase))
            {
                return new XirophtDecentralizedCpuMiner(threadId, threadConfiguration, cancellationToken, pool);
            }

            throw new NotImplementedException($"{pool.Algorithm} is not implemented.");
        }

        [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId")]
        private static extern int GetCurrentThreadId_Windows();

        [DllImport("Kernel32", EntryPoint = "OpenThread")]
        private static extern IntPtr OpenThread_Windows(int desiredAccess, bool inheritHandle, int threadId);

        [DllImport("kernel32", EntryPoint = "SetThreadAffinityMask")]
        private static extern UIntPtr SetThreadAffinityMask_Windows(IntPtr hThread, UIntPtr dwThreadAffinityMask);

        [DllImport("libc", EntryPoint = "sched_setaffinity")]
        private static extern int SetThreadAffinityMask_Linux(int pid, int cpuSetSize, ref ulong mask);

        public void StartMining()
        {
            _thread.Start();
        }

        public void StopMining()
        {
            _cancellationTokenSource.Cancel();
            _thread.Join();
        }

        public void UpdateBlockTemplate(IJobTemplate jobTemplate)
        {
            _jobTemplateQueue.Add(jobTemplate);
            _jobCancellationTokenSource.Cancel();
        }

        protected void LogCurrentJob(string message, params object[] args)
        {
            JobLog?.Invoke(message, true, args);
        }

        protected void SubmitJobResult(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            JobResultFound?.Invoke(jobTemplate, jobResult);
        }

        protected void IncrementHashCalculated()
        {
            Interlocked.Increment(ref _totalHashCalculatedIn10Seconds);
            Interlocked.Increment(ref _totalHashCalculatedIn60Seconds);
            Interlocked.Increment(ref _totalHashCalculatedIn15Minutes);
        }

        protected abstract void ExecuteJob(IJobTemplate jobTemplate, CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _jobTemplateQueue.Dispose();
                _cancellationTokenSource.Dispose();
                _jobCancellationTokenSource.Dispose();
            }
        }

        private void ConfigureMiningThread()
        {
            var threadAffinity = _threadConfiguration.GetThreadAffinity();

            try
            {
                Thread.BeginThreadAffinity();

                if (threadAffinity != 0)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var threadPtr = OpenThread_Windows(96, false, GetCurrentThreadId_Windows());
                        SetThreadAffinityMask_Windows(threadPtr, new UIntPtr(threadAffinity));
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        SetThreadAffinityMask_Linux(0, sizeof(ulong), ref threadAffinity);
                    }
                }

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var newJobTemplate = _jobTemplateQueue.Take();

                    while (_jobTemplateQueue.TryTake(out var jobTemplate))
                    {
                        newJobTemplate = jobTemplate;
                    }

                    _jobCancellationTokenSource.Dispose();
                    _jobCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

                    ExecuteJob(newJobTemplate, _jobCancellationTokenSource.Token);
                }
            }
            finally
            {
                Thread.EndThreadAffinity();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}