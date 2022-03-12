using System;
namespace SnifferGunbound
{
    public class SpecialSHA
    {
        private struct DWORD
        {
            public byte B0;
            public byte B1;
            public byte B2;
            public byte B3;
        }
        private byte[] m_ByteArray;
        private long m_HighByte;
        private long m_HighBound;
        private void Reset()
        {
            this.m_HighByte = 0L;
            this.m_HighBound = 1024L;
            this.m_ByteArray = new byte[1024];
        }
        private SpecialSHA.DWORD AndW(SpecialSHA.DWORD w1, SpecialSHA.DWORD w2)
        {
            return new SpecialSHA.DWORD
            {
                B0 = (byte)(w1.B0 & w2.B0),
                B1 = (byte)(w1.B1 & w2.B1),
                B2 = (byte)(w1.B2 & w2.B2),
                B3 = (byte)(w1.B3 & w2.B3)
            };
        }
        private SpecialSHA.DWORD OrW(SpecialSHA.DWORD w1, SpecialSHA.DWORD w2)
        {
            return new SpecialSHA.DWORD
            {
                B0 = (byte)(w1.B0 | w2.B0),
                B1 = (byte)(w1.B1 | w2.B1),
                B2 = (byte)(w1.B2 | w2.B2),
                B3 = (byte)(w1.B3 | w2.B3)
            };
        }
        private SpecialSHA.DWORD XOrW(SpecialSHA.DWORD w1, SpecialSHA.DWORD w2)
        {
            return new SpecialSHA.DWORD
            {
                B0 = (byte)(w1.B0 ^ w2.B0),
                B1 = (byte)(w1.B1 ^ w2.B1),
                B2 = (byte)(w1.B2 ^ w2.B2),
                B3 = (byte)(w1.B3 ^ w2.B3)
            };
        }
        private SpecialSHA.DWORD NotW(SpecialSHA.DWORD w)
        {
            SpecialSHA.DWORD result;
            result.B0 = (byte)(~w.B0);
            result.B1 = (byte)(~w.B1);
            result.B2 = (byte)(~w.B2);
            result.B3 = (byte)(~w.B3);
            return result;
        }
        private void Append(byte data)
        {
            if (1L + this.m_HighByte > this.m_HighBound)
            {
                this.m_HighBound += 1024L;
            }
            this.m_ByteArray[(int)checked((IntPtr)this.m_HighByte)] = data;
            this.m_HighByte += 1L;
        }
        private void Append(string data)
        {
            long num = (long)data.Length;
            if (num + this.m_HighByte > this.m_HighBound)
            {
                this.m_HighBound += 1024L;
            }
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
            int num2 = 0;
            while ((long)num2 < num)
            {
                this.m_ByteArray[(int)checked((IntPtr)unchecked(this.m_HighByte + (long)num2))] = bytes[num2];
                num2++;
            }
            this.m_HighByte += num;
        }
        public char Chr(byte i)
        {
            char[] chars = System.Text.Encoding.ASCII.GetChars(new byte[]
			{
				i
			}, 0, 1);
            return chars[0];
        }
        public string String(long times, string repeat)
        {
            string text = "";
            for (long num = 0L; num < times; num += 1L)
            {
                text += repeat;
            }
            return text;
        }
        private string Space(long count)
        {
            string text = "";
            int num = 0;
            while ((long)num < count)
            {
                text += " ";
                num++;
            }
            return text;
        }
        private byte[] GData()
        {
            byte[] array = new byte[this.m_HighByte];
            int num = 0;
            while ((long)num < this.m_HighByte)
            {
                array[num] = this.m_ByteArray[num];
                num++;
            }
            return array;
        }
        private SpecialSHA.DWORD CircShiftLeftW(SpecialSHA.DWORD w, int n)
        {
            uint num = this.DWORDToUINT(w);
            uint num2 = num;
            num = (uint)(num * Math.Pow(2.0, (double)n));
            num2 = (uint)(num2 / Math.Pow(2.0, (double)(32 - n)));
            return this.OrW(this.ToDWORD(num), this.ToDWORD(num2));
        }
        private SpecialSHA.DWORD AddW(SpecialSHA.DWORD w1, SpecialSHA.DWORD w2)
        {
            int num = (int)(w1.B3 + w2.B3);
            SpecialSHA.DWORD result;
            result.B3 = (byte)(num % 256);
            num = (int)(w1.B2 + w2.B2) + num / 256;
            result.B2 = (byte)(num % 256);
            num = (int)(w1.B1 + w2.B1) + num / 256;
            result.B1 = (byte)(num % 256);
            num = (int)(w1.B0 + w2.B0) + num / 256;
            result.B0 = (byte)(num % 256);
            return result;
        }
        public byte[] SHA1(string inMsg)
        {
            SpecialSHA.DWORD[] array = new SpecialSHA.DWORD[4];
            SpecialSHA.DWORD[] array2 = new SpecialSHA.DWORD[5];
            SpecialSHA.DWORD[] array3 = new SpecialSHA.DWORD[80];
            long num = (long)inMsg.Length;
            SpecialSHA.DWORD dWORD = this.ToDWORD((uint)(num * 8L));
            this.Reset();
            int num2 = (int)(128L - num % 64L - 9L) % 64;
            int num3 = inMsg.Length + 9 + num2;
            this.Append(inMsg);
            this.Append(128);
            for (int i = 0; i < num2 + 4; i++)
            {
                this.Append(0);
            }
            this.Append(dWORD.B0);
            this.Append(dWORD.B1);
            this.Append(dWORD.B2);
            this.Append(dWORD.B3);
            byte[] array4 = this.GData();
            this.Reset();
            long num4 = (long)(array4.Length / 64);
            array[0] = this.ToDWORD(1518500249u);
            array[1] = this.ToDWORD(1859775393u);
            array[2] = this.ToDWORD(2400959708u);
            array[3] = this.ToDWORD(3395469782u);
            array2[0] = this.ToDWORD(1732584193u);
            array2[1] = this.ToDWORD(4023233417u);
            array2[2] = this.ToDWORD(2562383102u);
            array2[3] = this.ToDWORD(271733878u);
            array2[4] = this.ToDWORD(3285377520u);
            int num5 = 0;
            while ((long)num5 < num4)
            {
                byte[] array5 = new byte[64];
                for (int j = num5 * 64; j < (num5 + 1) * 64; j++)
                {
                    array5[j % 64] = array4[j];
                }
                for (int k = 0; k <= 15; k++)
                {
                    array3[k].B0 = array5[k * 4];
                    array3[k].B1 = array5[k * 4 + 1];
                    array3[k].B2 = array5[k * 4 + 2];
                    array3[k].B3 = array5[k * 4 + 3];
                }
                for (int k = 16; k <= 79; k++)
                {
                    array3[k] = this.XOrW(this.XOrW(this.XOrW(array3[k - 3], array3[k - 8]), array3[k - 14]), array3[k - 16]);
                }
                SpecialSHA.DWORD dWORD2 = array2[0];
                SpecialSHA.DWORD dWORD3 = array2[1];
                SpecialSHA.DWORD dWORD4 = array2[2];
                SpecialSHA.DWORD dWORD5 = array2[3];
                SpecialSHA.DWORD w = array2[4];
                for (int k = 0; k <= 79; k++)
                {
                    SpecialSHA.DWORD dWORD6 = this.AddW(this.AddW(this.AddW(this.AddW(this.CircShiftLeftW(dWORD2, 5), this.F(k, dWORD3, dWORD4, dWORD5)), w), array3[k]), array[k / 20]);
                    w = dWORD5;
                    dWORD5 = dWORD4;
                    dWORD4 = this.CircShiftLeftW(dWORD3, 30);
                    dWORD3 = dWORD2;
                    dWORD2 = dWORD6;
                }
                array2[0] = this.AddW(array2[0], dWORD2);
                array2[1] = this.AddW(array2[1], dWORD3);
                array2[2] = this.AddW(array2[2], dWORD4);
                array2[3] = this.AddW(array2[3], dWORD5);
                array2[4] = this.AddW(array2[4], w);
                num5++;
            }
            return new byte[]
			{
				array2[0].B3,
				array2[0].B2,
				array2[0].B1,
				array2[0].B0,
				array2[1].B3,
				array2[1].B2,
				array2[1].B1,
				array2[1].B0,
				array2[2].B3,
				array2[2].B2,
				array2[2].B1,
				array2[2].B0,
				array2[3].B3,
				array2[3].B2,
				array2[3].B1,
				array2[3].B0
			};
        }
        private uint DWORDToUINT(SpecialSHA.DWORD w)
        {
            return (uint)((int)w.B0 << 24 | (int)w.B1 << 16 | (int)w.B2 << 8 | (int)w.B3);
        }
        private SpecialSHA.DWORD ToDWORD(uint n)
        {
            SpecialSHA.DWORD result;
            result.B0 = (byte)(n >> 24);
            result.B1 = (byte)(n >> 16);
            result.B2 = (byte)(n >> 8);
            result.B3 = (byte)n;
            return result;
        }
        private SpecialSHA.DWORD F(int t, SpecialSHA.DWORD b, SpecialSHA.DWORD c, SpecialSHA.DWORD d)
        {
            SpecialSHA.DWORD result;
            if (t <= 19)
            {
                result = this.OrW(this.AndW(b, c), this.AndW(this.NotW(b), d));
            }
            else
            {
                if (t <= 39)
                {
                    result = this.XOrW(this.XOrW(b, c), d);
                }
                else
                {
                    if (t <= 59)
                    {
                        result = this.OrW(this.OrW(this.AndW(b, c), this.AndW(b, d)), this.AndW(c, d));
                    }
                    else
                    {
                        result = this.XOrW(this.XOrW(b, c), d);
                    }
                }
            }
            return result;
        }
    }
}
