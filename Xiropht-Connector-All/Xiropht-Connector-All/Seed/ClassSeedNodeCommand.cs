namespace Xiropht_Connector_All.Seed
{
    public class ClassSeedNodeCommand
    {
        public class ClassSendSeedEnumeration
        {
            public const string WalletAskRemoteNode = "WALLET-ASK-REMOTE-NODE";
            public const string RemoteAskToBePublic = "REMOTE-ASK-TO-BE-PUBLIC";
            public const string RemoteAskOwnIP = "REMOTE-ASK-OWN-IP";
            public const string WalletCheckMaxSupply = "WALLET-CHECK-MAX-SUPPLY";
            public const string WalletCheckCoinCirculating = "WALLET-CHECK-COIN-CIRCULATING";
            public const string WalletCheckTotalTransactionFee = "WALLET-CHECK-TOTAL-TRANSACTION-FEE";
            public const string WalletCheckTotalBlockMined = "WALLET-CHECK-TOTAL-BLOCK-MINED";
            public const string WalletCheckNetworkHashrate = "WALLET-CHECK-NETWORK-HASHRATE";
            public const string WalletCheckNetworkDifficulty = "WALLET-CHECK-NETWORK-DIFFICULTY";
            public const string WalletCheckTotalPendingTransaction = "WALLET-CHECK-TOTAL-PENDING-TRANSACTION";
            public const string WalletCheckBlockPerId = "WALLET-CHECK-BLOCK-PER-ID";
        }

        public class ClassReceiveSeedEnumeration
        {
            public const string WalletSendRemoteNode = "WALLET-SEND-REMOTE-NODE";
            public const string WalletSendSeedNode = "WALLET-SEND-SEED-NODE";
            public const string RemoteSendOwnIP = "REMOTE-SEND-OWN-IP";
            public const string DisconnectPacket = "DISCONNECT";
            public const string WalletResultMaxSupply = "WALLET-RESULT-MAX-SUPPLY";
            public const string WalletResultCoinCirculating = "WALLET-RESULT-COIN-CIRCULATING";
            public const string WalletResultTotalTransactionFee = "WALLET-RESULT-TOTAL-TRANSACTION-FEE";
            public const string WalletResultTotalBlockMined = "WALLET-RESULT-TOTAL-BLOCK-MINED";
            public const string WalletResultNetworkHashrate = "WALLET-RESULT-NETWORK-HASHRATE";
            public const string WalletResultNetworkDifficulty = "WALLET-RESULT-NETWORK-DIFFICULTY";
            public const string WalletResultTotalPendingTransaction = "WALLET-RESULT-TOTAL-PENDING-TRANSACTION";
            public const string WalletResultBlockPerId = "WALLET-RESULT-BLOCK-PER-ID";
        }
    }
}