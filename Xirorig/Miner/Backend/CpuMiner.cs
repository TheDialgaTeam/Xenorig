using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Xirorig.Algorithms.Xiropht.Decentralized;
using Xirorig.Miner.Network.Api.JobResult;
using Xirorig.Miner.Network.Api.JobTemplate;
using Xirorig.Options;

namespace Xirorig.Miner.Backend
{
    internal delegate void CpuMinerJobResultFound(IJobTemplate jobTemplate, IJobResult jobResult);

    internal delegate void CpuMinerException(Exception exception);

    internal abstract class CpuMiner : IDisposable
    {
        private static class Native
        {
            [DllImport("kernel32", EntryPoint = "GetCurrentThreadId")]
            public static extern int GetCurrentThreadId_Windows();

            [DllImport("kernel32", EntryPoint = "OpenThread")]
            public static extern IntPtr OpenThread_Windows(int desiredAccess, bool inheritHandle, int threadId);

            [DllImport("kernel32", EntryPoint = "SetThreadAffinityMask")]
            public static extern UIntPtr SetThreadAffinityMask_Windows(IntPtr hThread, in ulong dwThreadAffinityMask);

            [DllImport("libc", EntryPoint = "sched_setaffinity")]
            public static extern int SetThreadAffinityMask_Linux(int pid, int cpuSetSize, in ulong mask);
        }

        public event CpuMinerJobResultFound? JobResultFound;
        public event CpuMinerException? Error;

        private readonly CpuMinerThreadConfiguration _threadConfiguration;
        private readonly CancellationToken _globalCancellationToken;
        private readonly BlockingCollection<IJobTemplate> _jobTemplateQueue = new();

        private Thread? _thread;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _jobCancellationTokenSource;

        private long _totalHashCalculated;

        public long TotalHashCalculated
        {
            get => Environment.Is64BitProcess ? _totalHashCalculated : Interlocked.Read(ref _totalHashCalculated);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculated = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculated, value);
                }
            }
        }

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private CancellationToken JobCancellationToken => _jobCancellationTokenSource.Token;

        protected CpuMiner(CpuMinerThreadConfiguration threadConfiguration, CancellationToken cancellationToken)
        {
            _threadConfiguration = threadConfiguration;
            _globalCancellationToken = cancellationToken;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _jobCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
        }

        public static CpuMiner CreateCpuMiner(Pool pool, CpuMinerThreadConfiguration threadConfiguration, CancellationToken cancellationToken)
        {
            var poolAlgorithm = pool.GetAlgorithm();

            if (poolAlgorithm.Equals("xiropht_decentralized", StringComparison.OrdinalIgnoreCase))
            {
                return new XirophtDecentralizedCpuMiner(pool, threadConfiguration, cancellationToken);
            }

            throw new NotImplementedException($"{pool.Algorithm} is not implemented.");
        }

        public void StartMining()
        {
            if (_thread != null) return;

            _thread = new Thread(ConfigureMiningThread) { IsBackground = true, Priority = _threadConfiguration.GetThreadPriority() };
            _thread.Start();
        }

        public void StopMining()
        {
            var initialValue = _cancellationTokenSource;
            var value = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationToken);

            if (Interlocked.CompareExchange(ref _cancellationTokenSource, value, initialValue) != initialValue)
            {
                value.Dispose();
                return;
            }

            initialValue.Cancel();
            initialValue.Dispose();

            _thread = null;
        }

        public void UpdateBlockTemplate(IJobTemplate jobTemplate)
        {
            _jobTemplateQueue.Add(jobTemplate, CancellationToken);
            _jobCancellationTokenSource.Cancel();
        }

        protected void SubmitJobResult(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            JobResultFound?.Invoke(jobTemplate, jobResult);
        }

        protected void IncrementHashCalculated()
        {
            Interlocked.Increment(ref _totalHashCalculated);
        }

        protected abstract void ExecuteJob(IJobTemplate jobTemplate, CancellationToken cancellationToken);

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
                        var threadPtr = Native.OpenThread_Windows(96, false, Native.GetCurrentThreadId_Windows());
                        Native.SetThreadAffinityMask_Windows(threadPtr, threadAffinity);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Native.SetThreadAffinityMask_Linux(0, sizeof(ulong), threadAffinity);
                    }
                }

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var newJobTemplate = _jobTemplateQueue.Take(CancellationToken);

                    while (_jobTemplateQueue.TryTake(out var jobTemplate))
                    {
                        newJobTemplate = jobTemplate;
                    }

                    _jobCancellationTokenSource.Dispose();
                    _jobCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

                    ExecuteJob(newJobTemplate, JobCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _jobTemplateQueue.Dispose();
            _cancellationTokenSource.Dispose();
            _jobCancellationTokenSource.Dispose();
        }
    }
}