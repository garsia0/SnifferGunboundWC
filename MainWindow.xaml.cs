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

                                /*
                                Temp = "";
                                for (int i = 0; i < PacketB.Length; i++)
                                {
                                    Temp += "0x" + PacketB[i].ToString("X2") + ",";
                                }
                                DataScreen += Temp + Environment.NewLine;
                                */

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

                                    Crypto C = new Crypto(Login, GetPassword, BitConverter.ToUInt32(AuthDWORD, 0));
                                    byte[] DynData = PR.PReadBytes(32);
                                    byte[] RealData = new byte[24];
                                    bool PDecryptStatus = C.PacketDecrypt(DynData, ref RealData, 4112);

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






                                    //Login
                                }

                                if (CD == 4113)
                                {
                                    Crypto Crypto;
                                    Crypto.Initialize();

                                    byte[] UserBytes = new byte[16];
                                    Array.Copy(PacketB, 6, UserBytes, 0, 16);
                                    UserBytes = Crypto.DecryptStaticBuffer(UserBytes);
                                    String Username = Encoding.ASCII.GetString(UserBytes);
                                    //DataScreen += "[Client]>Decripted User:" + Username + Environment.NewLine;

                                    byte[] UserCrypto = new byte[16];
                                    Array.Copy(PacketB, 22, UserCrypto, 0, 16);
                                    UserCrypto = Crypto.DecryptStaticBuffer(UserCrypto);

                                    DataScreen += "[Client]>Decripted Salt:" + Environment.NewLine;
                                    DataScreen += "[Client]>Decripted UserName:" + Username + Environment.NewLine;
                                    DataScreen += Utils.HexDump(UserCrypto, 16) + Environment.NewLine;



                                    byte[] PasswordData = new byte[32];
                                    byte[] PasswordDataO = new byte[32];
                                    Array.Copy(PacketB, 38, PasswordData, 0, 32);

                                    Crypto = new Crypto("aleks", "remakegbxd", Salt);

                                    Crypto.PacketDecrypt(PasswordData, ref PasswordDataO, 4113);

                                    DataScreen += "[Client]>Decripted Password:" + Environment.NewLine;
                                    DataScreen += Utils.HexDump(PasswordDataO, 16) + Environment.NewLine;

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
