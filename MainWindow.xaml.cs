using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SnifferGunbound
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread Windows;
        UInt16 Salt;
        bool Start = false;

        List<int> Port = new List<int>();

        Dictionary<int, Crypto> CryptoPort = new Dictionary<int, Crypto>();

        IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void UpdateTCP(String Data)
        {
            if (this.Dispatcher.CheckAccess())
            {
                TXTTCPLOG.Text += Data + Environment.NewLine;
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UpdateTCP(Data); }));
            }
        }

        public String GetPassword
        {
            get 
            {
                String Temp = String.Empty;
                if (this.Dispatcher.CheckAccess())
                {
                    Temp = TXTGBP.Text;
                }
                else
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { Temp = TXTGBP.Text; })); ;
                }

                return Temp;
            }
        }

       

        

        public void UpdateUDP(String Data)
        {
            if (this.Dispatcher.CheckAccess())
            {
                TXTUDPLOG.Text += Data + Environment.NewLine;
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UpdateUDP(Data); }));
            }
        }

        public void PasswordError(bool Data)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (Data)
                {
                    TXTGBP.BorderBrush = Brushes.Red;
                }
                else
                {
                    TXTGBP.ClearValue(TextBox.BorderBrushProperty);
                }
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { PasswordError(Data); }));
            }
        }

        private void TXTTCPClear_Click(object sender, RoutedEventArgs e)
        {
            TXTTCPLOG.Text = String.Empty;
        }

        private void TXTUDPClear_Click(object sender, RoutedEventArgs e)
        {
            TXTUDPLOG.Text = String.Empty;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Crypto.Initialize();

            /*
            byte[] D = new byte[] { 0x46, 0x00, 0xDF, 0xCD, 0x11, 0x10, 0x4E, 0x44, 0x66, 0x3A, 0x61, 0xFF, 0xDB, 0xF9, 0xB8, 0xA6, 0xD1, 0xFB, 0xD6, 0xC0, 0x2B, 0x6A, 0xA7, 0xC0, 0xB7, 0xAE, 0xBA, 0xE8, 0xCE, 0xBC, 0x91, 0x2C, 0xC9, 0xA1, 0x52, 0xD4, 0x1A, 0x5A, 0xFE, 0x60, 0x5D, 0xF9, 0xE2, 0xE4, 0xBD, 0x57, 0xB0, 0xAB, 0xA7, 0x09, 0x72, 0x70, 0xED, 0x19, 0x5F, 0xD3, 0x04, 0xC7, 0xE0, 0xAD, 0xFA, 0x87, 0x17, 0x2B, 0x14, 0x5A, 0x0F, 0x16, 0xCA, 0x7C };

            PacketReader PDP = new PacketReader(D);
            byte[] Leng = PDP.PReadBytes(2);
            byte[] SQ = PDP.PReadBytes(2);
            byte[] CD = PDP.PReadBytes(2);
            byte[] LE = PDP.PReadBytes(16);
            byte[] ESalt = PDP.PReadBytes(16);
            byte[] PlayLoad = PDP.PReadBytes(32);


            byte[] LD = Crypto.DecryptStaticBuffer(LE);
            byte[] DSalt = Crypto.DecryptStaticBuffer(ESalt);
            uint S = BitConverter.ToUInt32(DSalt,0);
            String Login = Encoding.ASCII.GetString(LD).TrimEnd('\0');

            Crypto C = new Crypto(Login, "1234", S);

            byte[] xD = new byte[16];

            Array.Copy(PlayLoad, 0, xD, 0, 16);

            byte[] DPlayLoad = C.DecryptDynamic(xD);

            String X = Encoding.ASCII.GetString(DPlayLoad);

          */

         

            BTNAPPLY_Click(null, null);

            if (allDevices.Count == 0)
            {
                ComboAdapter.Items.Add("No interfaces found! Make sure WinPcap is installed.");
            }

            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                ComboAdapter.Items.Add(device.Description);
            }

        }

        private void BTNAPPLY_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TXTPORT.Text))
            {
                Port.Clear();
                String[] LP = TXTPORT.Text.Split(',');
                
                for (int i = 0; i < LP.Length; i++)
                {
                    int CPort = 0;
                    if (Int32.TryParse(LP[i], out CPort))
                    {
                        Port.Add(CPort);
                    }
                }
            }
           

        }

        private void BTNSTART_Click(object sender, RoutedEventArgs e)
        {
            if (Start)
            {
                Windows.Abort();
                BTNSTART.Content = "Start";
                Start = false;
            }
            else
            {
                if (ComboAdapter.SelectedIndex != -1)
                {
                    try
                    {
                        PacketData.SDevice = allDevices[ComboAdapter.SelectedIndex];
                    }
                    catch
                    {
                        UpdateTCP("Cant open this Device!" + Environment.NewLine);
                        return;
                    }

                    if (PacketData.SDevice == null)
                    {
                        UpdateTCP("Select device!" + Environment.NewLine);
                        return;
                    }

                    Windows = new Thread(WindowsT);
                    Windows.Start();
                    BTNSTART.Content = "Stop";
                    Start = true;
                }
                else
                {
                    UpdateTCP("Select Device!" + Environment.NewLine);
                }
            }
        }

        private void PacketHandler(Packet packet)
        {

            MemoryStream PacketData;
            byte[] PacketB;
            String DataScreen = "", DataScreenUDP = "", Temp;
            IpV4Datagram Ip = packet.Ethernet.IpV4;
            TcpDatagram Tcp = Ip.Tcp;
            UdpDatagram Udp = Ip.Udp;
            UInt16 OpCode;

            if (Tcp != null)
            {
                if (Port.Contains(Tcp.DestinationPort) | Port.Contains(Tcp.SourcePort))
                {
                    try
                    {
                        if (Tcp.Payload != null)
                        {
                            PacketData = Tcp.Payload.ToMemoryStream();
                            PacketB = PacketData.ToArray();



                            if (PacketData.Length > 2 & (Port.Contains(Tcp.SourcePort)))
                            {
                                DataScreen += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                                DataScreen += "Source:" + Ip.Source + ":" + Ip.Tcp.SourcePort + " Destination:" + Ip.Destination + ":" + Ip.Tcp.DestinationPort + Environment.NewLine;
                                DataScreen += "[SERVER]>Send Data Size Leng:" + PacketData.Length + Environment.NewLine;

                                UInt16 SQ = BitConverter.ToUInt16(PacketB, 2);
                                DataScreen += "[SERVER]>Send SQ:" + SQ + Environment.NewLine;

                                UInt16 CD = BitConverter.ToUInt16(PacketB, 4);
                                DataScreen += "[SERVER]>Send CD:" + CD + Environment.NewLine;

                                if (CD == 4097)
                                {
                                    Salt = BitConverter.ToUInt16(PacketB, 6);
                                    DataScreen += "[SERVER]>Send Salt:" + Salt + Environment.NewLine;
                                }

                                
                                Temp = "";
                                for (int i = 0; i < PacketB.Length; i++)
                                {
                                    Temp += "0x" + PacketB[i].ToString("X2") + ",";
                                }
                                DataScreen += Temp + Environment.NewLine;
                                

                                DataScreen += Utils.HexDump(PacketB, 16) + Environment.NewLine;



                            }
                            else if (PacketData.Length > 2)
                            {
                                DataScreen += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                                DataScreen += "Source:" + Ip.Source + ":" + Ip.Tcp.SourcePort + " Destination:" + Ip.Destination + ":" + Ip.Tcp.DestinationPort + Environment.NewLine;

                                DataScreen += "[Client]>Send Data Size Leng:" + PacketData.Length + Environment.NewLine;
                                PacketReader PR = new PacketReader(PacketB);
                                Int16 Leng = PR.PReadInt16(); //Leng
                                Int16 SQ = PR.PReadInt16(); //SQ
                                Int16 CD = PR.PReadInt16(); //CD
                                DataScreen += "[Client]>Send SQ:" + SQ + Environment.NewLine;
                                DataScreen += "[Client]>Send CD:" + CD + Environment.NewLine;



                                if (CD == 4112)
                                {
                                        
                                 

                                    Crypto.Initialize();
                                    byte[] CryptedDataUser = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));
                                    String Login = Encoding.ASCII.GetString(CryptedDataUser).TrimEnd('\0');
                                    DataScreen += "[Client]>Send Login: " + Login + Environment.NewLine;
                                    byte[] AuthDWORD = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));



                                    if (CryptoPort.ContainsKey(Ip.Tcp.DestinationPort))
                                    {
                                        CryptoPort[Ip.Tcp.DestinationPort] = new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0));
                                    }
                                    else
                                    {
                                        CryptoPort.Add(Ip.Tcp.DestinationPort, new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0)));
                                    }


                                    byte[] DynData = PR.PReadBytes(32);
                                    byte[] RealData = new byte[24];
                                    bool PDecryptStatus = CryptoPort[Ip.Tcp.DestinationPort].PacketDecrypt(DynData, ref RealData, 4112);

                                    if (PDecryptStatus)
                                    {
                                        PasswordError(false);
                                        PacketReader LoginData = new PacketReader(RealData);
                                        String Password = Encoding.ASCII.GetString(LoginData.PReadBytes(20)).TrimEnd('\0');
                                        uint Version = LoginData.PReadUInt32();
                                        DataScreen += "[Client]>Send Password: " + Password + Environment.NewLine;
                                        DataScreen += "[Client]>Send Version: " + Version + Environment.NewLine;
                                    }
                                    else
                                    {
                                        PasswordError(true);
                                        DataScreen += "[Client]>Send Password: (Cant Decrypt) Invalid Password ?" + Environment.NewLine;
                                    }

                                }

                                if (CD == 4113)
                                {
                                      Temp = "";
                                   for (int i = 0; i < PacketB.Length; i++)
                                   {
                                       Temp += "0x" + PacketB[i].ToString("X2") + ",";
                                   }
                                   DataScreen += Temp + Environment.NewLine;
                                   

                                    Crypto.Initialize();
                                    byte[] CryptedDataUser = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));
                                    String Login = Encoding.ASCII.GetString(CryptedDataUser).TrimEnd('\0');
                                    DataScreen += "[Client]>Send Login: " + Login + Environment.NewLine;
                                    byte[] AuthDWORD = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));

                                    DataScreen += "Salt" + Utils.HexDump(AuthDWORD, 16) + Environment.NewLine;

                                    if (CryptoPort.ContainsKey(Ip.Tcp.DestinationPort))
                                    {
                                        CryptoPort[Ip.Tcp.DestinationPort] = new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0));
                                    }
                                    else
                                    {
                                        CryptoPort.Add(Ip.Tcp.DestinationPort, new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0)));
                                    }


                                    byte[] DynData = PR.PReadBytes(32);

                                   


                                    byte[] RealData = CryptoPort[Ip.Tcp.DestinationPort].DecryptDynamic(DynData);

                                    DataScreen += "Data" + Utils.HexDump(RealData, 16) + Environment.NewLine;


                                    bool PDecryptStatus = CryptoPort[Ip.Tcp.DestinationPort].PacketDecrypt(DynData, ref RealData, 4113);

                                    if (PDecryptStatus)
                                    {
                                        
                                        PasswordError(false);
                                        PacketReader LoginData = new PacketReader(RealData);
                                        String Password = Encoding.ASCII.GetString(LoginData.PReadBytes(16)).TrimEnd('\0');
                                        DataScreen += "[Client]>Send Password: " + Password + Environment.NewLine;

                                    }
                                    else
                                    {
                                        PasswordError(true);
                                        DataScreen += "[Client]>Send Password: (Cant Decrypt) Invalid Password ?" + Environment.NewLine;
                                    }

                                }

                                if (CD == 8208)
                                {
                                    DataScreen += "[Client]>Send Channel Msg:" + Environment.NewLine;
                                    if (CryptoPort.ContainsKey(Ip.Tcp.DestinationPort))
                                    {
                                        byte[] Playload = PR.PReadBytes(PR.Length - 6);
                                        byte[] RealData = new byte[PR.Length - 6];
                                        if (CryptoPort[Ip.Tcp.DestinationPort].PacketDecrypt(Playload, ref RealData, 8208))
                                        {
                                            PacketReader MsgData = new PacketReader(RealData);
                                            byte L = MsgData.PReadByte();
                                            String Texto = MsgData.PReadStringLeng(L);
                                            DataScreen += "[Client]>Send Channel Msg Content:" + Texto + Environment.NewLine;
                                        }
                                       
                                    }
                                        
                                   

                                }

                                DataScreen += Utils.HexDump(PacketB, 16) + Environment.NewLine;
                            }

                            if (!String.IsNullOrEmpty(DataScreen))
                            {
                                UpdateTCP(DataScreen);
                            }
                        }
                    }
                    catch
                    { }

                }
            }

            if (Udp != null)
            {
                if (true)
                {

                    if (Port.Contains(Udp.DestinationPort) | Port.Contains(Udp.SourcePort))
                    {

                        MemoryStream MsRowData = Udp.Payload.ToMemoryStream();
                        byte[] RowData = MsRowData.ToArray();

                        DataScreenUDP += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                        DataScreenUDP += Ip.Source + ":" + Udp.SourcePort + " -> " + Ip.Destination + ":" + Udp.DestinationPort + Environment.NewLine;
                        DataScreenUDP += Utils.HexDump(RowData) + Environment.NewLine;
                        
                        /*
                        Temp = "";
                        for (int i = 0; i < RowData.Length; i++)
                        {
                            Temp += "0x" + RowData[i].ToString("X2") + ",";
                        }
                        DataScreenUDP += Temp + Environment.NewLine;
                        */


                        if (!String.IsNullOrEmpty(DataScreenUDP))
                        {
                            UpdateUDP(DataScreenUDP);
                        }
                    }
                }
            }


        }
        private void WindowsT()
        {
            using (PacketCommunicator Communicator = PacketData.SDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))                                  // read timeout
            {
                Communicator.ReceivePackets(0, PacketHandler);
            }

        }

       
    }
}
