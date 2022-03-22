
using SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        bool Start = false;

        List<ushort> Port = new List<ushort>();

        Dictionary<int, Crypto> CryptoPort = new Dictionary<int, Crypto>();

        ICaptureDevice Current = null;

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

            BTNAPPLY_Click(null, null);

            var devices = CaptureDeviceList.Instance;



            if (devices.Count == 0)
            {
                ComboAdapter.Items.Add("No interfaces found! Make sure WinPcap is installed.");
            }

            for (int i = 0; i != CaptureDeviceList.Instance.Count; ++i)
            {
                ComboAdapter.Items.Add(CaptureDeviceList.Instance[i].Description);
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
                    ushort CPort = 0;
                    if (ushort.TryParse(LP[i], out CPort))
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
                BTNSTART.Content = "Start";
                Start = false;
                if (Current != null)
                {
                    Current.StopCapture();
                    Current = null;
                }
            }
            else
            {
                if (ComboAdapter.SelectedIndex != -1)
                {
                    int Index = ComboAdapter.SelectedIndex;
                    try
                    {
                        Task.Run(async () =>
                        {
                            Current = CaptureDeviceList.Instance[Index];
                            Current.OnPacketArrival += Current_OnPacketArrival;
                            Current.Open(DeviceModes.Promiscuous | DeviceModes.DataTransferUdp, 1000);
                            Current.Capture();
                            
                            
                        });
                    }
                    catch
                    {
                        UpdateTCP("Cant Open This Device!" + Environment.NewLine);
                        return;
                    }

                    BTNSTART.Content = "Stop";
                    Start = true;

                }
                else
                {
                    UpdateTCP("Select Device!" + Environment.NewLine);
                }
            }
        }

        private void Current_OnPacketArrival(object sender, PacketCapture e)
        {
            var time = e.Header.Timeval.Date;
            var len = e.Data.Length;
            var rawPacket = e.GetPacket();

            var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var ipV4Packet = packet.Extract<PacketDotNet.IPv4Packet>();
            if (ipV4Packet != null)
            {
                System.Net.IPAddress srcIp = ipV4Packet.SourceAddress;
                System.Net.IPAddress dstIp = ipV4Packet.DestinationAddress;

                var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();
                if (udpPacket != null)
                {

                    ushort SourcePort = udpPacket.SourcePort;
                    ushort DestinationPort = udpPacket.DestinationPort;
                    if ((Port.Contains(SourcePort) | Port.Contains(DestinationPort)) && udpPacket.HasPayloadData)
                    {
                        String DataScreenUDP = String.Empty;
                        byte[] Raw = udpPacket.PayloadData;
                        DataScreenUDP += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                        DataScreenUDP += srcIp.ToString() + ":" + SourcePort + " -> " + dstIp.ToString() + ":" + DestinationPort + Environment.NewLine;
                        DataScreenUDP += Utils.HexDump(Raw);

                        /*
                        String Temp = String.Empty;
                        for (int i = 0; i < Raw.Length; i++)
                        {
                            Temp += "0x" + Raw[i].ToString("X2") + ",";
                        }
                        DataScreenUDP += Temp + Environment.NewLine;
                        */


                        if (!String.IsNullOrEmpty(DataScreenUDP))
                        {
                            UpdateUDP(DataScreenUDP);
                        }
                    }

                }

                var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
                if (tcpPacket != null)
                {
                    ushort SourcePort = tcpPacket.SourcePort;
                    ushort DestinationPort = tcpPacket.DestinationPort;
                    if ((Port.Contains(SourcePort) | Port.Contains(DestinationPort)) && tcpPacket.HasPayloadData)
                    {
                        String DataScreen = String.Empty;
                        byte[] Raw = tcpPacket.PayloadData;
                        try
                        {

                            if (Raw.Length > 2 & (Port.Contains(SourcePort)))
                            {
                                DataScreen += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                                DataScreen += "Source:" + srcIp.ToString() + ":" + SourcePort + " Destination:" + dstIp.ToString() + ":" + DestinationPort + Environment.NewLine;
                                DataScreen += "[SERVER]>Send Data Size Leng:" + Raw.Length + Environment.NewLine;

                                PacketReader PR = new PacketReader(Raw);
                                UInt16 Leng = PR.PReadUInt16(); //Length
                                UInt16 SQ = PR.PReadUInt16();
                                UInt16 CD = PR.PReadUInt16();
                                DataScreen += "[Server]>Send LN:" + Leng + Environment.NewLine;
                                DataScreen += "[SERVER]>Send SQ:" + SQ + Environment.NewLine;
                                DataScreen += "[SERVER]>Send CD:" + CD + Environment.NewLine;

                                if (CD == 4097)
                                {
                                    int AuthDWORD = PR.PReadUInt16();
                                    DataScreen += "[SERVER]>Send AuthDWORD:" + AuthDWORD + Environment.NewLine;
                                }

                                /*
                                String Temp = "";
                                for (int i = 0; i < Raw.Length; i++)
                                {
                                    Temp += "0x" + Raw[i].ToString("X2") + ",";
                                }
                                DataScreen += Temp + Environment.NewLine;
                                */

                                DataScreen += Utils.HexDump(Raw, 16) + Environment.NewLine;



                            }
                            else if (Raw.Length > 2)
                            {
                                DataScreen += " Time:" + DateTime.Now.ToString("HH:mm:ss:") + DateTime.Now.Millisecond + Environment.NewLine;
                                DataScreen += "Source:" + srcIp.ToString() + ":" + SourcePort + " Destination:" + dstIp.ToString() + ":" + DestinationPort + Environment.NewLine;

                                DataScreen += "[Client]>Send Data Size Leng:" + Raw.Length + Environment.NewLine;
                                PacketReader PR = new PacketReader(Raw);
                                Int16 Leng = PR.PReadInt16(); //Leng
                                Int16 SQ = PR.PReadInt16(); //SQ
                                Int16 CD = PR.PReadInt16(); //CD
                                DataScreen += "[Client]>Send LN:" + Leng + Environment.NewLine;
                                DataScreen += "[Client]>Send SQ:" + SQ + Environment.NewLine;
                                DataScreen += "[Client]>Send CD:" + CD + Environment.NewLine;

                                if (CD == 4112)
                                {
                                    Crypto.Initialize();
                                    byte[] CryptedDataUser = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));
                                    String Login = Encoding.ASCII.GetString(CryptedDataUser).TrimEnd('\0');
                                    DataScreen += "[Client]>Send Login: " + Login + Environment.NewLine;
                                    byte[] AuthDWORD = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));



                                    if (CryptoPort.ContainsKey(DestinationPort))
                                    {
                                        CryptoPort[DestinationPort] = new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0));
                                    }
                                    else
                                    {
                                        CryptoPort.Add(DestinationPort, new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0)));
                                    }


                                    byte[] DynData = PR.PReadBytes(32);
                                    byte[] RealData = new byte[24];
                                    bool PDecryptStatus = CryptoPort[DestinationPort].PacketDecrypt(DynData, ref RealData, 4112);

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
                                    /*
                                    String Temp = "";
                                    for (int i = 0; i < Raw.Length; i++)
                                    {
                                        Temp += "0x" + Raw[i].ToString("X2") + ",";
                                    }
                                    DataScreen += Temp + Environment.NewLine;
                                    */

                                    Crypto.Initialize();
                                    byte[] CryptedDataUser = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));
                                    String Login = Encoding.ASCII.GetString(CryptedDataUser).TrimEnd('\0');
                                    DataScreen += "[Client]>Send Login: " + Login + Environment.NewLine;
                                    byte[] AuthDWORD = Crypto.DecryptStaticBuffer(PR.PReadBytes(16));

                                    DataScreen += "Salt" + Utils.HexDump(AuthDWORD, 16) + Environment.NewLine;

                                    if (CryptoPort.ContainsKey(DestinationPort))
                                    {
                                        CryptoPort[DestinationPort] = new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0));
                                    }
                                    else
                                    {
                                        CryptoPort.Add(DestinationPort, new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0)));
                                    }


                                    byte[] DynData = PR.PReadBytes(32);




                                    byte[] RealData = CryptoPort[DestinationPort].DecryptDynamic(DynData);
                                    DataScreen += "Data:" + Utils.HexDump(RealData, 16) + Environment.NewLine;
                                    bool PDecryptStatus = CryptoPort[DestinationPort].PacketDecrypt(DynData, ref RealData, 4113);

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
                                    if (CryptoPort.ContainsKey(DestinationPort))
                                    {
                                        byte[] Playload = PR.PReadBytes(PR.Length - 6);
                                        byte[] RealData = new byte[PR.Length - 6];
                                        if (CryptoPort[DestinationPort].PacketDecrypt(Playload, ref RealData, 8208))
                                        {
                                            PacketReader MsgData = new PacketReader(RealData);
                                            byte L = MsgData.PReadByte();
                                            String Texto = MsgData.PReadStringLeng(L);
                                            DataScreen += "[Client]>Send Channel Msg Content:" + Texto + Environment.NewLine;
                                        }

                                    }

                                }

                                DataScreen += Utils.HexDump(Raw, 16) + Environment.NewLine;
                            }

                            if (!String.IsNullOrEmpty(DataScreen))
                            {
                                UpdateTCP(DataScreen);
                            }

                        }
                        catch
                        {
                            UpdateTCP("Error: Read");
                        }
                    }

                }
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
