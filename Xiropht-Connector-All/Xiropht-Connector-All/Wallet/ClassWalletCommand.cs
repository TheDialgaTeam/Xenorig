namespace Xiropht_Connector_All.Wallet
{
    public class ClassWalletCommand
    {
        public class ClassWalletSendEnumeration
        {
            /// <summary>
            /// First 
            /// </summary>
            public const string CreatePhase = "WALLET-CREATE"; // Phase 0
            public const string LoginPhase = "WALLET-LOGIN-SEND"; // Phase 0
            public const string AskPhase = "WALLET-ASK"; // Phase 0

            /// <summary>
            /// Other request.
            /// </summary>
            public const string PasswordPhase = "WALLET-PASSWORD-SEND"; // Phase 1
            public const string KeyPhase = "WALLET-KEY-SEND"; // Phase 2
            public const string PinPhase = "WALLET-PIN-SEND"; // Phase 3
            public const string SendTransaction = "WALLET-TRANSACTION-SEND"; // Phase 4
            public const string KeepAlive = "KEEP-ALIVE"; // All phase
            public const string ChangePassword = "WALLET-CHANGE-PASSWORD"; // Phase 4
            public const string DisablePinCode = "WALLET-DISABLE-PIN-CODE"; // Phase 4
        }

        public class ClassWalletReceiveEnumeration
        {
            public const string WaitingHandlePacket = "WALLET-HANDLE-PACKET"; // Every phase
            public const string DisconnectPacket = "WALLET-DISCONNECT-PACKET"; // Every Phase
            public const string WalletCreatePasswordNeedMoreCharacters = "WALLET-CREATE-PASSWORD-NEED-MORE-CHARACTERS"; // Every Phase
            public const string WalletCreatePasswordNeedLetters = "WALLET-CREATE-PASSWORD-NEED-LETTERS-CHARACTERS"; // Every Phase
            public const string WaitingCreatePhase = "WALLET-CREATE-WATING"; // Phase 0
            public const string WalletAlreadyConnected = "WALLET-ALREADY-CONNECTED"; // Every phase
            public const string RightPhase = "WALLET-RIGHT-SUCCESS"; // Phase 0
            public const string CreatePhase = "WALLET-CREATE-SUCCESS"; // Phase 0
            public const string LoginPhase = "WALLET-LOGIN-REQUIRED"; // Phase 0
            public const string WalletAskSuccess = "WALLET-ASK-SUCCESS"; // Phase 0
            public const string PasswordPhase = "WALLET-PASSWORD-REQUIRED"; // Phase 1
            public const string KeyPhase = "WALLET-KEY-REQUIRED"; // Phase 2
            public const string PinPhase = "WALLET-PIN-REQUIRED"; // Phase 3
            public const string PinAcceptedPhase = "WALLET-PIN-ACCEPTED"; // Phase 3
            public const string PinRefusedPhase = "WALLET-PIN-REFUSED"; // Phase 3
            public const string WalletBanPhase = "WALLET-BAN"; // Phase 3
            public const string LoginAcceptedPhase = "WALLET-LOGIN-ACCEPTED"; // Phase 4
            public const string StatsPhase = "WALLET-STATS"; // Phase 4
            public const string AmountNotValid = "WALLET-AMOUNT-NOT-VALID"; // Phase 4
            public const string AddressNotValid = "WALLET-ADDRESS-NOT-VALID"; // Phase 4
            public const string AmountInsufficient = "WALLET-AMOUNT-INSUFFICIENT"; // Phase 4
            public const string FeeInsufficient = "WALLET-FEE-INSUFFICIENT"; // Phase 4
            public const string TransactionAccepted = "WALLET-TRANSACTION-ACCEPTED"; // Phase 4
            public const string WalletChangePasswordAccepted = "WALLET-CHANGE-PASSWORD-ACCEPTED"; // Phase 4
            public const string WalletChangePasswordRefused = "WALLET-CHANGE-PASSWORD-REFUSED"; // Phase 4
            public const string WalletDisablePinCodeAccepted = "WALLET-DISABLE-PIN-CODE-ACCEPTED"; // Phase 4
            public const string WalletDisablePinCodeRefused = "WALLET-DISABLE-PIN-CODE-REFUSED"; // Phase 4
            public const string WalletWarningConnection = "WALLET-WARNING-CONNECTION"; // Phase 4
            public const string WalletSendTransactionBusy = "WALLET-SEND-TRANSACTION-BUSY"; // Phase 4
            public const string WalletReceiveTransactionBusy = "WALLET-RECEIVE-TRANSACTION-BUSY"; // Phase 4
            public const string WalletSendMessage = "WALLET-SEND-MESSAGE"; // Phase 4
            public const string WalletSendTransactionData = "WALLET-SEND-TRANSACTION-DATA"; // Phase 4
            public const string WalletNewGenesisKey = "WALLET-NEW-GENESIS-KEY"; // Phase 4

            public const string WalletSendTotalPendingTransactionOnReceive =
                "WALLET-SEND-TOTAL-PENDING-TRANSACTION-ON-RECEIVE"; // Phase 4

        }
    }
}