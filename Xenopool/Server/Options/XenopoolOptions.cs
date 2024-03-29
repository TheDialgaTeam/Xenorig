﻿using JetBrains.Annotations;
using Xenolib.Utilities;

namespace Xenopool.Server.Options;

public sealed class XenopoolOptions
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public RemoteNode RemoteNode
    {
        get => _remoteNode!;
        private set => _remoteNode = value ?? _remoteNode;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public RpcWallet RpcWallet
    {
        get => _rpcWallet!;
        private set => _rpcWallet = value ?? _rpcWallet;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public SoloMining SoloMining
    {
        get => _soloMining!;
        private set => _soloMining = value ?? _soloMining;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public Pool Pool
    {
        get => _pool!;
        private set => _pool = value ?? _pool;
    }

    private RemoteNode? _remoteNode = new();
    private RpcWallet? _rpcWallet = new();
    private SoloMining? _soloMining = new();
    private Pool? _pool = new();
}

public sealed class RemoteNode
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string Host
    {
        get => _host!;
        private set => _host = string.IsNullOrEmpty(value) ? _host : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int Port { get; private set; } = 18001;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string UserAgent
    {
        get => _userAgent!;
        private set => _userAgent = string.IsNullOrEmpty(value) ? _userAgent : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int NetworkTimeoutDuration { get; private set; } = 5;

    private string? _host = "127.0.0.1";
    private string? _userAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";
}

public sealed class RpcWallet
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string Host
    {
        get => _host!;
        private set => _host = string.IsNullOrEmpty(value) ? _host : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int Port { get; private set; } = 18001;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string EncryptionKey
    {
        get => _encryptionKey!;
        private set => _encryptionKey = string.IsNullOrEmpty(value) ? _encryptionKey : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string WalletAddress
    {
        get => _walletAddress!;
        private set => _walletAddress = string.IsNullOrEmpty(value) ? _walletAddress : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string UserAgent
    {
        get => _userAgent!;
        private set => _userAgent = string.IsNullOrEmpty(value) ? _userAgent : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int NetworkTimeoutDuration { get; private set; } = 5;

    private string? _host = "127.0.0.1";
    private string? _userAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";
    private string? _walletAddress = string.Empty;
    private string? _encryptionKey = string.Empty;
}

public sealed class SoloMining
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string Host
    {
        get => _host!;
        private set => _host = string.IsNullOrEmpty(value) ? _host : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int Port { get; private set; } = 18001;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string UserAgent
    {
        get => _userAgent!;
        private set => _userAgent = string.IsNullOrEmpty(value) ? _userAgent : value;
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int NetworkTimeoutDuration { get; private set; } = 5;

    private string? _host = "127.0.0.1";
    private string? _userAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";
}

public sealed class Pool
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong MinimumPayoutAmount { get; private set; } = 1001;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int MaximumJobSolutions { get; private set; } = 1000;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int BanUserAfterFailedShares { get; private set; } = 5;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int BanDuration { get; private set; } = 3600;
}