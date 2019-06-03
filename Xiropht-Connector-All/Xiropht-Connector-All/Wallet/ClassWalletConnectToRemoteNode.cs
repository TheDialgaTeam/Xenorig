using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Remote;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Connector_All.Wallet
{

    public class ClassWalletConnectToRemoteNodeObjectSendPacket : IDisposable
    {
        public byte[] packetByte;
        private bool disposed;

        public ClassWalletConnectToRemoteNodeObjectSendPacket(string packet)
        {
            packetByte = ClassUtils.GetByteArrayFromString(packet);
        }

        ~ClassWalletConnectToRemoteNodeObjectSendPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
        }
    }

    public class ClassWalletConnectToRemoteNodeObjectPacket : IDisposable
    {
        public byte[] buffer;
        public string packet;
        private bool disposed;

        public ClassWalletConnectToRemoteNodeObjectPacket()
        {
            buffer = new byte[ClassConnectorSetting.MaxNetworkPacketSize];
            packet = string.Empty;
        }

        ~ClassWalletConnectToRemoteNodeObjectPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
        }
    }

    public class ClassWalletConnectToRemoteNodeObjectError
    {
        public const string ObjectError = "ERROR";
        public const string ObjectNone = "NONE";
    }

    public class ClassWalletConnectToRemoteNodeObject
    {
        public const string ObjectAskBlock = "BLOCK";
        public const string ObjectTransaction = "TRANSACTION";
        public const string ObjectSupply = "SUPPLY";
        public const string ObjectCirculating = "CIRCULATING";
        public const string ObjectFee = "FEE";
        public const string ObjectBlockMined = "MINED";
        public const string ObjectDifficulty = "DIFFICULTY";
        public const string ObjectRate = "RATE";
        public const string ObjectPendingTransaction = "PENDING-TRANSACTION";
        public const string ObjectAskWalletTransaction = "ASK-TRANSACTION";
        public const string ObjectAskLastBlockFound = "ASK-LAST-BLOCK-FOUND";
        public const string ObjectAskWalletAnonymityTransaction = "ASK-ANONYMITY-TRANSACTION";
    }

    public class ClassWalletConnectToRemoteNode : IDisposable
    {
        private TcpClient _remoteNodeClient;
        private string _remoteNodeClientType;
        public string RemoteNodeHost;
        public bool RemoteNodeStatus;
        public int TotalInvalidPacket;
        private bool disposed;
        public long LastTrustDate;

        ~ClassWalletConnectToRemoteNode()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _remoteNodeClient = null;
            }
            disposed = true;
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="remoteNodeType"></param>
        public ClassWalletConnectToRemoteNode(string remoteNodeType)
        {
            _remoteNodeClientType = remoteNodeType;
            RemoteNodeStatus = true;
        }

        /// <summary>
        /// Return the connection status opened to a remote node.
        /// </summary>
        /// <returns></returns>
        public bool CheckRemoteNode()
        {
            return RemoteNodeStatus;
        }

        /// <summary>
        ///     Connect the wallet to a remote node.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<bool> ConnectToRemoteNodeAsync(string host, int port, bool isLinux = false)
        {
            MalformedPacket = string.Empty;

            try
            {
                _remoteNodeClient?.Close();
            }
            catch
            {

            }
            RemoteNodeHost = host;
            TotalInvalidPacket = 0;
            LastTrustDate = 0;
            try
            {
                RemoteNodeStatus = true;
                _remoteNodeClient = new TcpClient();
                if(!await ConnectToTarget(host, port))
                {
                    RemoteNodeStatus = false;
                    return false;
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Console.WriteLine("Error to connect wallet on remote nodes: " + error.Message);
#endif
                RemoteNodeStatus = false;
                return false;
            }

            RemoteNodeHost = host;

            _remoteNodeClient.SetSocketKeepAliveValues(20 * 60 * 1000, 30 * 1000);

            new Thread(delegate () { EnableCheckConnection(); }).Start();
            
            return true;
        }

        /// <summary>
        /// Check the connection opened to the remote node.
        /// </summary>
        /// <param name="isLinux"></param>
        private async void EnableCheckConnection()
        {
            while (RemoteNodeStatus)
            {
                try
                {
                    if (!ClassUtils.SocketIsConnected(_remoteNodeClient))
                    {
                        RemoteNodeStatus = false;
                        break;
                    }
                    else
                    {
                        if (!await SendPacketRemoteNodeAsync(ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.KeepAlive + "|0"))
                        {
                            RemoteNodeStatus = false;
                            break;
                        }
                    }

                }
                catch
                {
                    RemoteNodeStatus = false;
                    break;
                }
                Thread.Sleep(5000);
            }
        }


        private async Task<bool> ConnectToTarget(string host, int port)
        {

            var clientTask = _remoteNodeClient.ConnectAsync(host, port);
            var delayTask = Task.Delay(ClassConnectorSetting.MaxTimeoutConnectRemoteNode);

            var completedTask = await Task.WhenAny(new[] { clientTask, delayTask });
            return completedTask == clientTask;

        }


        private string MalformedPacket;

        /// <summary>
        ///     Listen network of remote node.
        /// </summary>
        [HostProtection(ExternalThreading = true)]
        public async Task<string> ListenRemoteNodeNetworkAsync()
        {
            try
            {

                using (var _remoteNodeStream = new NetworkStream(_remoteNodeClient.Client))
                {
                    using (var bufferedStreamNetwork = new BufferedStream(_remoteNodeStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        using (var bufferPacket = new ClassWalletConnectToRemoteNodeObjectPacket())
                        {
                            int received = await bufferedStreamNetwork.ReadAsync(bufferPacket.buffer, 0, bufferPacket.buffer.Length);
                            if (received > 0)
                            {
                                string packet = ClassUtils.GetStringFromByteArray(bufferPacket.buffer, received);
                                if (packet.Contains("*"))
                                {
                                    if (!string.IsNullOrEmpty(MalformedPacket))
                                    {
                                        packet = MalformedPacket + packet;
                                        MalformedPacket = string.Empty;
                                    }
                                    return packet;
                                }
                                else
                                {
                                    if (MalformedPacket.Length -1 >= int.MaxValue || MalformedPacket.Length + packet.Length >= int.MaxValue)
                                    {
                                        MalformedPacket = string.Empty;
                                    }
                                    MalformedPacket += packet;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                _remoteNodeClient?.Close();
                _remoteNodeClient?.Dispose();
                RemoteNodeStatus = false;
#if DEBUG
                Console.WriteLine("Error to listen remote node network: " + error.Message);
#endif
                return ClassWalletConnectToRemoteNodeObjectError.ObjectError;
            }

            return ClassWalletConnectToRemoteNodeObjectError.ObjectNone;
        }

        /// <summary>
        ///     Disconnect wallet of remote node.
        /// </summary>
        public void DisconnectRemoteNodeClient()
        {
            MalformedPacket = string.Empty;
            _remoteNodeClient?.Close();
            _remoteNodeClientType = string.Empty;
            TotalInvalidPacket = 0;
            LastTrustDate = 0;
            Dispose();
        }

        /// <summary>
        ///     Send a selected command to remote node.
        /// </summary>
        /// <param name="command"></param>
        [HostProtection(ExternalThreading = true)]
        public async Task<bool> SendPacketRemoteNodeAsync(string command)
        {
            var clientTask = TaskSendPacketRemoteNode(command);
            var delayTask = Task.Delay(ClassConnectorSetting.MaxTimeoutSendPacket);

            var completedTask = await Task.WhenAny(new[] { clientTask, delayTask });
            return completedTask == clientTask;
        }

        private async Task<bool> TaskSendPacketRemoteNode(string command)
        {
            if (!RemoteNodeStatus)
            {
                return false;
            }
            try
            {
                using (var _remoteNodeStream = new NetworkStream(_remoteNodeClient.Client))
                {
                    using (var bufferedStream = new BufferedStream(_remoteNodeStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        using (var packetObject = new ClassWalletConnectToRemoteNodeObjectSendPacket(command + "*"))
                        {
                            await bufferedStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                            await bufferedStream.FlushAsync();
                        }
                    }
                }

            }
            catch
            {
                RemoteNodeStatus = false;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Send the right packet type to remote node.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendPacketTypeRemoteNode(string walletId)
        {
           ClassWalletConnectToRemoteNodeObjectSendPacket packet;

            switch (_remoteNodeClientType)
            {
                case ClassWalletConnectToRemoteNodeObject.ObjectTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.WalletAskHisNumberTransaction +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskWalletAnonymityTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.WalletAskHisAnonymityNumberTransaction +
                        "|" + walletId + "*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectSupply:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCoinMaxSupply + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectCirculating:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCoinCirculating + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectFee:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalFee + "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectBlockMined:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalBlockMined + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectDifficulty:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCurrentDifficulty + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectRate:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCurrentRate + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectPendingTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalPendingTransaction +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskLastBlockFound:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskLastBlockFoundTimestamp +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskWalletTransaction:
                    return true;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskBlock:
                    return true;
                default:
                    return false;
            }


            try
            {
                using (var _remoteNodeStream = new NetworkStream(_remoteNodeClient.Client))
                {
                    using (var bufferedStreamNetwork = new BufferedStream(_remoteNodeStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        await bufferedStreamNetwork.WriteAsync(packet.packetByte, 0, packet.packetByte.Length);
                        await bufferedStreamNetwork.FlushAsync();
                    }
                }
               
            }
            catch (Exception error)
            {
                RemoteNodeStatus = false;
#if DEBUG
                Console.WriteLine("Error to send packet on remote node network: " + error.Message);
#endif
                return false;
            }


            return true;
        }
    }
}