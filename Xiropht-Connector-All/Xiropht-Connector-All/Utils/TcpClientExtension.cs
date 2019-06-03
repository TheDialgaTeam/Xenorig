using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Xiropht_Connector_All.Utils
{
    public static class TcpExtensionClass
    {
        public static void SetSocketKeepAliveValues(this TcpClient tcpc, int KeepAliveTime, int KeepAliveInterval)
        {
            uint dummy = 0; 
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            bool OnOff = true;

            BitConverter.GetBytes((uint)(OnOff ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)KeepAliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)KeepAliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            tcpc.Client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
    }
}
