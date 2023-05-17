using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.Database.Tables;
using Xenopool.Server.SoloMining;

namespace Xenopool.Server.Pool;

public sealed class PoolClient
{
    private readonly PoolAccount _poolAccount;
    private readonly string _workerId;

    private DateTime _lastRequestTime = DateTime.Now;

    private BlockHeaderResponse _blockHeaderResponse = new() { Status = false, Reason = "Blockchain is not ready." };
    private BlockHeaderResponse.Types.JobHeader _jobHeader = new();
    
    private readonly PoolShare[] _poolShares = new PoolShare[2];

    public PoolClient(PoolAccount poolAccount, string workerId)
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

    public BlockHeaderResponse GetBlockHeader(SoloMiningJob soloMiningJob)
    {
        if (_blockHeaderResponse.BlockHeader?.BlockIndication == soloMiningJob.BlockHeaderResponse.BlockHeader.BlockIndication)
        {
            if (_blockHeaderResponse.JobHeader?.JobIndications.Equals(_jobHeader.JobIndications) ?? false)
            {
                return _blockHeaderResponse;
            }
        }
        
        GenerateNewJobHeader(soloMiningJob);
        
        _blockHeaderResponse = soloMiningJob.BlockHeaderResponse.Clone();
        _blockHeaderResponse.JobHeader = _jobHeader;

        return _blockHeaderResponse;
    }

    private void GenerateNewJobHeader(SoloMiningJob soloMiningJob)
    {
        _jobHeader = new BlockHeaderResponse.Types.JobHeader();
        
        if (soloMiningJob.TryGenerateSemiRandomPoolShare(out _poolShares[0]))
        {
            _jobHeader.JobIndications.Add(_poolShares[0].EncryptedShareHash);
        }
        
        if (soloMiningJob.TryGenerateRandomPoolShare(out _poolShares[1]))
        {
            _jobHeader.JobIndications.Add(_poolShares[1].EncryptedShareHash);
        }
    }
}