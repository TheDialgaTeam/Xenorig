using System.Net.NetworkInformation;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Connector_All.Utils
{
    public class CheckPing
    {
        /// <summary>
        ///     Check the ping from host and return the ping time.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static int CheckPingHost(string host, bool checkSeed = false)
        {
            try
            {
                using (var pingTestNode = new Ping())
                {
                    var replyNode = pingTestNode.Send(host);
                    if (replyNode.Status == IPStatus.Success) return (int)replyNode.RoundtripTime;
                    else
                    {
                        if (checkSeed)
                        {
                            return ClassConnectorSetting.MaxTimeoutConnect;
                        }
                        else
                        {
                            return ClassConnectorSetting.MaxTimeoutConnectRemoteNode;
                        }
                    }
                }
            }
            catch
            {
                if (checkSeed)
                {
                    return ClassConnectorSetting.MaxTimeoutConnect;
                }
                else
                {
                    return ClassConnectorSetting.MaxTimeoutConnectRemoteNode;
                }
            }
        }
    }
}