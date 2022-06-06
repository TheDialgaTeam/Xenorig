namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm
{
    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    private const string NETWORK_GENESIS_SECONDARY_KEY = "XENOPHYTESEED";

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    private const int MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM = 256;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    private const int SeedNodePort = 18000;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSetting" />
    private const int SeedNodeTokenPort = 18003;

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSettingEnumeration" />
    private const string WalletTokenType = "WALLET-TOKEN";

    /// <see cref="Xenophyte_Connector_All.Setting.ClassConnectorSettingEnumeration" />
    private const string MinerLoginType = "MINER";

    /// <see cref="Xenophyte_Connector_All.RPC.ClassRpcWalletCommand" />
    private const string TokenCheckWalletAddressExist = "TOKEN-CHECK-WALLET-ADDRESS-EXIST";

    /// <see cref="Xenophyte_Connector_All.RPC.ClassRpcWalletCommand" />
    private const string SendTokenCheckWalletAddressInvalid = "SEND-TOKEN-CHECK-WALLET-ADDRESS-INVALID";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string SendLoginAccepted = "SEND-LOGIN-ACCEPTED";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string SendJobStatus = "SEND-JOB-STATUS";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string SendCurrentBlockMining = "SEND-CURRENT-BLOCK-MINING";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string SendContentBlockMethod = "SEND-CONTENT-BLOCK-METHOD";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string ShareWrong = "WRONG"; // Block not accepted, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string ShareUnlock = "UNLOCK"; // Block accepted, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string ShareAleady = "ALREADY"; // Block already mined, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration" />
    private const string ShareNotExist = "NOTEXIST"; // Block height not exist, only for solo mining and pool request.

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    private const string ReceiveJob = "RECEIVE-JOB";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    private const string ReceiveAskCurrentBlockMining = "RECEIVE-ASK-CURRENT-BLOCK-MINING";

    /// <see cref="Xenophyte_Connector_All.SoloMining.ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration" />
    private const string ReceiveAskContentBlockMethod = "RECEIVE-ASK-CONTENT-BLOCK-METHOD";
}