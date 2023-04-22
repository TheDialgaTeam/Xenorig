namespace Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;

public static class NetworkConstants
{
    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    public const string NetworkGenesisSecondaryKey = "XENOPHYTESEED";

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    public const int MajorUpdate1SecurityCertificateSizeItem = 256;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    public const int SeedNodePort = 18000;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    public const int SeedNodeTokenPort = 18003;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSettingEnumeration" />
    public const string WalletTokenType = "WALLET-TOKEN";

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSettingEnumeration" />
    public const string MinerLoginType = "MINER";

    /// <see cref="Xenophyte_Connector_All.RPC.ClassRpcWalletCommand" />
    public const string TokenCheckWalletAddressExist = "TOKEN-CHECK-WALLET-ADDRESS-EXIST";

    /// <see cref="Xenophyte_Connector_All.RPC.ClassRpcWalletCommand" />
    public const string SendTokenCheckWalletAddressInvalid = "SEND-TOKEN-CHECK-WALLET-ADDRESS-INVALID";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string SendLoginAccepted = "SEND-LOGIN-ACCEPTED";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string SendJobStatus = "SEND-JOB-STATUS";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string SendCurrentBlockMining = "SEND-CURRENT-BLOCK-MINING";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string SendContentBlockMethod = "SEND-CONTENT-BLOCK-METHOD";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string ShareWrong = "WRONG"; // Block not accepted, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string ShareUnlock = "UNLOCK"; // Block accepted, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string ShareAleady = "ALREADY"; // Block already mined, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    public const string ShareNotExist = "NOTEXIST"; // Block height not exist, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    public const string ReceiveJob = "RECEIVE-JOB";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    public const string ReceiveAskCurrentBlockMining = "RECEIVE-ASK-CURRENT-BLOCK-MINING";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    public const string ReceiveAskContentBlockMethod = "RECEIVE-ASK-CONTENT-BLOCK-METHOD";
}