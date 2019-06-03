namespace Xiropht_Connector_All.SoloMining
{
    public class ClassSoloMiningPacketEnumeration
    {
        public class SoloMiningRecvPacketEnumeration
        {
            public const string SendLoginAccepted = "SEND-LOGIN-ACCEPTED";
            public const string SendJobStatus = "SEND-JOB-STATUS";
            public const string SendCurrentBlockMining = "SEND-CURRENT-BLOCK-MINING";
            public const string SendListBlockMethod = "SEND-LIST-BLOCK-METHOD";
            public const string SendContentBlockMethod = "SEND-CONTENT-BLOCK-METHOD";

            public const string
                SendEnableCheckShare = "SEND-ENABLE-CHECK-SHARE"; // Only working with solo mining proxy.

            public const string
                SendDisableCheckShare = "SEND-DISABLE-CHECK-SHARE"; // Only working with solo mining proxy.

            public const string ShareWrong = "WRONG"; // Block not accepted, only for solo mining and pool request.
            public const string ShareUnlock = "UNLOCK"; // Block accepted, only for solo mining and pool request.
            public const string ShareAleady = "ALREADY"; // Block already mined, only for solo mining and pool request.

            public const string
                ShareNotExist = "NOTEXIST"; // Block height not exist, only for solo mining and pool request.

            public const string ShareGood = "GOOD"; // Work only with solo mining proxy.
            public const string ShareBad = "BAD"; // Work only with solo mining proxy.
        }

        public class SoloMiningSendPacketEnumeration
        {
            public const string ReceiveJob = "RECEIVE-JOB";
            public const string ReceiveAskCurrentBlockMining = "RECEIVE-ASK-CURRENT-BLOCK-MINING";
            public const string ReceiveAskListBlockMethod = "RECEIVE-ASK-LIST-BLOCK-METHOD";
            public const string ReceiveAskContentBlockMethod = "RECEIVE-ASK-CONTENT-BLOCK-METHOD";
            public const string KeepAlive = "KEEP-ALIVE"; // All phase
            public const string ShareHashrate = "SEND-MINER-HASHRATE"; // Work only with the solo mining proxy.

        }
    }
}