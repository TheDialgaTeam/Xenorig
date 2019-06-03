namespace Xiropht_Connector_All.Remote
{
    public class ClassRemoteNodeCommand
    {
        public class ClassRemoteNodeSendToSeedEnumeration
        {
            /// <summary>
            ///     Command for transactions
            /// </summary>
            public const string RemoteNumberOfTransaction = "ASK-NUMBER-OF-TRANSACTION";

            public const string RemoteAskTransactionPerId = "ASK-TRANSACTION-PER-ID";

            public const string RemoteAskTransactionPerRange = "ASK-TRANSACTION-PER-RANGE";


            /// <summary>
            ///     Command for blockchain status
            /// </summary>
            public const string RemoteAskCoinMaxSupply = "ASK-COIN-MAX-SUPPLY";

            public const string RemoteAskCoinCirculating = "ASK-COIN-CIRCULATING";
            public const string RemoteAskTotalBlockMined = "ASK-TOTAL-BLOCK-MINED";
            public const string RemoteAskTotalPendingTransaction = "ASK-TOTAL-PENDING-TRANSACTION";
            public const string RemoteAskCurrentDifficulty = "ASK-CURRENT-DIFFICULTY";
            public const string RemoteAskCurrentRate = "ASK-CURRENT-RATE";
            public const string RemoteAskTotalFee = "ASK-TOTAL-FEE";

            /// <summary>
            ///     Command for keep alive connection
            /// </summary>

            public const string RemoteKeepAlive = "KEEP-ALIVE"; 

            /// <summary>
            /// Command for check
            /// </summary>
            public const string RemoteAskBlockPerId = "ASK-BLOCK-PER-ID";

            public const string RemoteCheckBlockPerId = "CHECK-BLOCK-PER-ID";

            public const string RemoteCheckTransactionPerId = "CHECK-TRANSACTION-PER-ID";

            public const string RemoteCheckBlockHash = "CHECK-BLOCK-HASH";

            public const string RemoteCheckTransactionHash = "CHECK-TRANSACTION-HASH";

            public const string RemoteCheckTrustedKey = "CHECK-TRUSTED-KEY";

            public const string RemoteAskSchemaTransaction = "ASK-SCHEMA-TRANSACTION";

            public const string RemoteAskSchemaBlock = "ASK-SCHEMA-BLOCK";

        }

        public class ClassRemoteNodeRecvFromSeedEnumeration
        {
            /// <summary>
            ///     Command for transactions.
            /// </summary>
            public const string RemoteAcceptedLogin = "SEND-REMOTE-ACCEPTED-LOGIN";

            public const string RemoteSendNumberOfTransaction = "SEND-NUMBER-OF-TRANSACTION";
            public const string RemoteSendTransactionPerId = "SEND-TRANSACTION-PER-ID";
            public const string RemoteSendMissingTransactionId = "SEND-TRANSACTION-MISSING-ID";
            public const string RemoteSendWrongTransactionId = "SEND-TRANSACTION-WRONG-ID";

            /// <summary>
            ///     Command for blocks.
            /// </summary>
            public const string RemoteSendBlockPerId = "SEND-BLOCK-PER-ID";

            public const string RemoteSendWrongBlockId = "SEND-BLOCK-WRONG-ID";

            /// <summary>
            ///     Command for blockchain status.
            /// </summary>
            public const string RemoteSendCoinMaxSupply = "SEND-COIN-MAX-SUPPLY";

            public const string RemoteSendCoinCirculating = "SEND-COIN-CIRCULATING";
            public const string RemoteSendTotalBlockMined = "SEND-TOTAL-BLOCK-MINED";
            public const string RemoteSendTotalPendingTransaction = "SEND-TOTAL-PENDING-TRANSACTION";
            public const string RemoteSendCurrentDifficulty = "SEND-CURRENT-DIFFICULTY";
            public const string RemoteSendCurrentRate = "SEND-CURRENT-RATE";
            public const string RemoteSendTotalFee = "SEND-TOTAL-FEE";

            /// <summary>
            ///     Command for keep alive connection
            /// </summary>
            public const string RemoteKeepAlive = "KEEP-ALIVE";

            /// <summary>
            /// Command for check
            /// </summary>
            public const string RemoteSendAskBlockPerId = "SEND-ASK-BLOCK-PER-ID";
            public const string RemoteSendCheckBlockPerId = "SEND-CHECK-BLOCK-PER-ID";
            public const string RemoteSendCheckTransactionPerId = "SEND-CHECK-TRANSACTION-PER-ID";
            public const string RemoteSendCheckBlockHash = "SEND-CHECK-BLOCK-HASH";
            public const string RemoteSendCheckTransactionHash = "SEND-CHECK-TRANSACTION-HASH";
            public const string RemoteSendCheckTrustedKey = "SEND-CHECK-TRUSTED-KEY";
            public const string RemoteSendSchemaTransaction = "SEND-SCHEMA-TRANSACTION";
            public const string RemoteSendSchemaBlock = "SEND-SCHEMA-BLOCK";

        }
    }
}