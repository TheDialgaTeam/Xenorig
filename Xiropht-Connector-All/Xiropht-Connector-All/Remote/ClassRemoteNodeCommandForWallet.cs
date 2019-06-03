namespace Xiropht_Connector_All.Remote
{
    public class ClassRemoteNodeCommandForWallet
    {
        public class RemoteNodeRecvPacketEnumeration
        {
            public const string EmptyPacket = "EMPTY-PACKET";
            public const string WalletIdWrong = "WALLET-ID-WRONG-OR-EMPTY";
            public const string WalletWrongIdTransaction = "WALLET-WRONG-ID-TRANSACTION";
            public const string WalletYourNumberTransaction = "WALLET-YOUR-NUMBER-TRANSACTION";
            public const string WalletYourAnonymityNumberTransaction = "WALLET-YOUR-ANONYMITY-NUMBER-TRANSACTION";
            public const string WalletTotalNumberTransaction = "WALLET-TOTAL-TRANSACTION";
            public const string WalletTransactionPerId = "WALLET-TRANSACTION-PER-ID";
            public const string WalletAnonymityTransactionPerId = "WALLET-ANONYMITY-TRANSACTION-PER-ID";

            // Api command
            public const string SendRemoteNodeCoinMaxSupply = "SEND-REMOTE-NODE-COIN-MAX-SUPPLY";
            public const string SendRemoteNodeCoinCirculating = "SEND-REMOTE-NODE-COIN-CIRCULATING";
            public const string SendRemoteNodeTotalBlockMined = "SEND-REMOTE-NODE-TOTAL-BLOCK-MINED";
            public const string SendRemoteNodeTransactionPerId = "SEND-REMOTE-NODE-TRANSACTION-PER-ID";
            public const string SendRemoteNodeTransactionHashList = "SEND-REMOTE-NODE-TRANSACTION-HASH-LIST";
            public const string SendRemoteNodeTotalPendingTransaction = "SEND-REMOTE-NODE-TOTAL-PENDING-TRANSACTION";
            public const string SendRemoteNodeCurrentDifficulty = "SEND-REMOTE-NODE-CURRENT-DIFFICULTY";
            public const string SendRemoteNodeCurrentRate = "SEND-REMOTE-NODE-CURRENT-RATE";
            public const string SendRemoteNodeTotalBlockLeft = "SEND-REMOTE-NODE-TOTAL-BLOCK-LEFT";
            public const string SendRemoteNodeTotalFee = "SEND-REMOTE-NODE-TOTAL-FEE";
            public const string SendRemoteNodeTrustedKey = "SEND-REMOTE-NODE-TRUSTED-KEY";
            public const string SendRemoteNodeBlockPerId = "SEND-REMOTE-NODE-BLOCK-PER-ID";
            public const string SendRemoteNodeBlockHashList = "SEND-REMOTE-NODE-BLOCK-HASH-LIST";
            public const string SendRemoteNodeAskBlockHashPerId = "SEND-REMOTE-NODE-BLOCK-HASH-PER-ID";
            public const string SendRemoteNodeAskTransactionHashPerId = "SEND-REMOTE-NODE-TRANSACTION-HASH-PER-ID";
            public const string SendRemoteNodeKeepAlive = "KEEP-ALIVE";


            /// <summary>
            ///     10/08/2018 - MAJOR_UPDATE_1
            /// </summary>
            public const string SendRemoteNodeLastBlockFoundTimestamp = "SEND-REMOTE-NODE-LAST-BLOCK-FOUND-TIMESTAMP";
        }

        public class RemoteNodeSendPacketEnumeration
        {
            public const string WalletAskNumberTransaction = "WALLET-ASK-NUMBER-TRANSACTION";
            public const string WalletAskHisNumberTransaction = "WALLET-ASK-HIS-NUMBER-TRANSACTION";
            public const string WalletAskHisAnonymityNumberTransaction = "WALLET-ASK-HIS-ANONYMITY-NUMBER-TRANSACTION";
            public const string WalletAskTransactionPerId = "WALLET-ASK-TRANSACTION-PER-ID";
            public const string WalletAskAnonymityTransactionPerId = "WALLET-ASK-ANONYMITY-TRANSACTION-PER-ID";

            // Api command
            public const string AskCoinMaxSupply = "ASK-COIN-MAX-SUPPLY";
            public const string AskCoinCirculating = "ASK-COIN-CIRCULATING";
            public const string AskTotalBlockMined = "ASK-TOTAL-BLOCK-MINED";
            public const string AskTransactionPerId = "ASK-TRANSACTION-PER-ID";
            public const string AskTotalPendingTransaction = "ASK-TOTAL-PENDING-TRANSACTION";
            public const string AskCurrentDifficulty = "ASK-CURRENT-DIFFICULTY";
            public const string AskCurrentRate = "ASK-CURRENT-RATE";
            public const string AskTotalBlockLeft = "ASK-TOTAL-BLOCK-LEFT";
            public const string AskTotalFee = "ASK-TOTAL-FEE";
            public const string AskTrustedKey = "ASK-TRUSTED-KEY";
            public const string AskBlockPerId = "ASK-BLOCK-PER-ID";
            public const string AskHashListTransaction = "ASK-HASH-LIST-TRANSACTION";
            public const string AskHashListBlock = "ASK-HASH-LIST-BLOCK";
            public const string AskBlockHashPerId = "ASK-BLOCK-HASH-PER-ID";
            public const string AskTransactionHashPerId = "ASK-TRANSACTION-HASH-PER-ID";
            public const string WrongTransactionPerId = "WRONG-TRANSACTION-PER-ID";
            public const string WrongBlockPerId = "WRONG-BLOCK-PER-ID";
            public const string KeepAlive = "KEEP-ALIVE";

            /// <summary>
            ///     10/08/2018 - MAJOR_UPDATE_1
            /// </summary>
            public const string AskLastBlockFoundTimestamp = "ASK-LAST-BLOCK-FOUND-TIMESTAMP";
        }
    }
}