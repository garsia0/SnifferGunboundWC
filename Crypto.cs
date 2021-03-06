using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SnifferGunbound
{
    public class Crypto
    {
        private static Rijndael m_StaticRijndael;
        //private static byte[] m_StaticKey = { 0xA9, 0x27, 0x53, 0x04, 0x1B, 0xFC, 0xAC, 0xE6, 0x5B, 0x23, 0x38, 0x34, 0x68, 0x46, 0x03, 0x8C };//static key broker
        private static byte[] m_StaticKey = { 0xFF, 0xB3, 0xB3, 0xBE, 0xAE, 0x97, 0xAD, 0x83, 0xB9, 0x61, 0x0E, 0x23, 0xA4, 0x3C, 0x2E, 0xB0 };//static key gs
        private Rijndael m_DynamicRijndael;
        public static void Initialize()
        {
            Crypto.m_StaticRijndael = Rijndael.Create();
            Crypto.m_StaticRijndael.Key = Crypto.m_StaticKey;
            Crypto.m_StaticRijndael.Mode = CipherMode.ECB;
            Crypto.m_StaticRijndael.Padding = PaddingMode.Zeros;
        }
        private static byte[] DecryptStatic(byte[] cipherData)
        {
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, Crypto.m_StaticRijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherData, 0, cipherData.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
        public static byte[] EncryptStatic(string strData)
        {
            byte[] cipherData = GetBytes(strData);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, Crypto.m_StaticRijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherData, 0, cipherData.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
        public static byte[] EncryptStatic(byte[] cipherData)
        {
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, Crypto.m_StaticRijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherData, 0, cipherData.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
        public byte[] EncryptDynamic(byte[] cipherData)
        {
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, this.m_DynamicRijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherData, 0, cipherData.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
        public byte[] DecryptDynamic(byte[] cipherData)
        {
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, this.m_DynamicRijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherData, 0, cipherData.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
        public static byte[] DecryptStaticBuffer(byte[] decryptme)
        {
            byte[] result;
            if (decryptme.Length != 16)
            {
                Debug.WriteLine("DecyryptStatic() is 128-bit only.");
                result = null;
            }
            else
            {
                result = Crypto.DecryptStatic(decryptme);
            }
            return result;
        }
        public void Dispose()
        {
            this.m_DynamicRijndael.Clear();
            this.m_DynamicRijndael = null;
        }
        public bool PacketDecrypt(byte[] input, ref byte[] output, ushort packetid)
        {
            bool result;
            if (input.Length == 0)
            {
                Debug.WriteLine("Empty buffer passed for decryption");
                result = false;
            }
            else
            {
                if (input.Length % 16 != 0)
                {
                    Debug.WriteLine("Decrypt failed. Input byte count is not a multiple of 16.");
                    result = false;
                }
                else
                {
                    int num = input.Length / 16;
                    int num2 = num * 12;
                    byte[] array = new byte[input.Length];
                    byte[] array2 = new byte[num2];
                    array = this.DecryptDynamic(input);

                    uint num3 = 2251382910u;
                    for (int i = 0; i < num; i++)
                    {
                        uint num4 = BitConverter.ToUInt32(array, 16 * i);
                        if (num4 - (uint)packetid != num3)
                        {
                            Debug.WriteLine($"Bad Packet Signature. G: {num4.ToString("x8")} E: {(num3 + (uint)packetid).ToString("x8")}");
                            result = false;
                            return result;
                        }
                        for (int j = 4; j < 16; j++)
                        {
                            array2[i * 12 + (j - 4)] = array[i * 16 + j];
                        }
                    }
                    output = array2;
                    result = true;
                }
            }
            return result;
        }
        public Crypto(string login, string pass, uint dword)
        {
            if (login.Length <= 16 && pass.Length <= 20)
            {
                SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
                SpecialSHA specialSHA = new SpecialSHA();
                String text = String.Empty;
                byte[] AuthDword = GetBytes(dword);
                Array.Reverse(AuthDword);
                List<byte> Temp = new List<byte>();
                Temp.AddRange(Encoding.ASCII.GetBytes(login + pass));
                Temp.AddRange(AuthDword);
                byte[] Key = specialSHA.SHA1(Temp.ToArray());
                this.m_DynamicRijndael = Rijndael.Create();
                this.m_DynamicRijndael.Key = Key;
                this.m_DynamicRijndael.Mode = CipherMode.ECB;
                this.m_DynamicRijndael.Padding = PaddingMode.Zeros;
            }
        }

        public static byte[] GetBytes(string s)
        {
            byte[] bytes = new byte[s.Length];
            int index = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bytes[index++] = (byte)c;
            }
            return bytes;
        }

        public static byte[] GetBytes(ushort u)
        {
            byte[] bytes = new byte[2];
            for (int c = 1; c <= 2; c++)
            {
                bytes[c - 1] = (byte)(u >> 16 - c * 8);
            }
            return bytes;
        }
        public static byte[] GetBytes(long s)
        {
            byte[] bytes = new byte[8];
            for (int c = 1; c <= 8; c++)
            {
                bytes[c - 1] = (byte)((ulong)s >> 64 - c * 8);
            }
            return bytes;
        }
        public static byte[] GetBytes(uint t)
        {
            byte[] bytes = new byte[4];
            for (int c = 1; c <= 4; c++)
            {
                bytes[c - 1] = (byte)(t >> 32 - c * 8);
            }
            return bytes;
        }
    }
}
