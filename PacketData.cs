using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcapDotNet;
using PcapDotNet.Core;
using PcapDotNet.Base;
using PcapDotNet.Packets;

namespace SnifferGunbound
{
    class PacketData
    {
        private static PacketData Instance = new PacketData();

        private static PacketDevice VSDevice;
        private static UInt32 VShif;
        private static String VPort;
        private static String VIP;

        public static PacketData GetInstance()
        {
            return Instance;
        }

        public static PacketDevice SDevice
        {
            get { return VSDevice; }
            set { VSDevice = value; }
        }

        public static UInt32 Shif
        {
            get { return VShif; }
            set { VShif = value; }
        }

        public static String Port
        {
            get { return VPort; }
            set { VPort = value; }
        }

        public static String IP
        {
            get { return VIP; }
            set { VIP = value; }
        }

    }
}
