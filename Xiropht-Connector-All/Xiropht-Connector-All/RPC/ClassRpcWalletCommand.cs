namespace Xiropht_Connector_All.RPC
{
    public class ClassRpcWalletCommand
    {
        /// <summary>
        /// Request.
        /// </summary>
        public const string TokenAsk = "TOKEN-ASK";
        public const string TokenAskBalance = "TOKEN-ASK-BALANCE";
        public const string TokenAskWalletId = "TOKEN-ASK-WALLET-ID";
        public const string TokenAskWalletAnonymousId = "TOKEN-ASK-WALLET-ANONYMOUS-ID";
        public const string TokenAskWalletSendTransaction = "TOKEN-ASK-WALLET-SEND-TRANSACTION";
        public const string TokenCheckWalletAddressExist = "TOKEN-CHECK-WALLET-ADDRESS-EXIST";

        /// <summary>
        /// Reponse.
        /// </summary>
        public const string SendToken = "SEND-TOKEN";
        public const string SendTokenBalance = "SEND-TOKEN-BALANCE";
        public const string SendTokenWalletId = "SEND-TOKEN-WALLET-ID";
        public const string SendTokenWalletAnonymousId = "SEND-TOKEN-WALLET-ANONYMOUS-ID";
        public const string SendTokenExpired = "SEND-TOKEN-EXPIRED";
        public const string SendTokenCheckWalletAddressValid = "SEND-TOKEN-CHECK-WALLET-ADDRESS-VALID";
        public const string SendTokenCheckWalletAddressInvalid = "SEND-TOKEN-CHECK-WALLET-ADDRESS-INVALID";

        // Response of transaction.
        public const string SendTokenTransactionConfirmed = "SEND-TOKEN-TRANSACTION-CONFIRMED";
        public const string SendTokenTransactionRefused = "SEND-TOKEN-TRANSACTION-REFUSED";
        public const string SendTokenTransactionBusy = "SEND-TOKEN-TRANSACTION-BUSY";
        public const string SendTokenTransactionInvalidTarget = "SEND-TOKEN-TRANSACTION-INVALID-TARGET";
    }
}
