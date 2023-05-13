using Xenopool.Server.Database.Repository;
using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Pool;

public sealed class PoolClient : IDisposable
{
    public sealed class Worker
    {
        private readonly PoolAccount _poolAccount;
        private readonly string _workerId;
        
        private DateTime _lastRequestTime = DateTime.Now;

        public Worker(PoolAccount poolAccount, string workerId)
        {
            _poolAccount = poolAccount;
            _workerId = workerId;
        }

        public bool IsExpire()
        {
            return (DateTime.Now - _lastRequestTime).TotalMinutes > 1;
        }

        public void Ping()
        {
            _lastRequestTime = DateTime.Now;
        }
    }
    
    public PoolAccount Account { get; }

    private readonly PoolAccountRepository _poolAccountRepository;

    public PoolClient(PoolAccountRepository poolAccountRepository, string walletAddress)
    {
        _poolAccountRepository = poolAccountRepository;
        Account = poolAccountRepository.GetAccount(walletAddress) ?? poolAccountRepository.CreateAccount(walletAddress);
    }

    public void Dispose()
    {
        _poolAccountRepository.Dispose();
    }
}