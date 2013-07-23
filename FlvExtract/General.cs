using System;

namespace FlvExtract
{
    public static class BitConverterBE
    {
        public static byte[] GetBytes(ulong value)
        {
            var buff = new byte[8];
            buff[0] = (byte)(value >> 56);
            buff[1] = (byte)(value >> 48);
            buff[2] = (byte)(value >> 40);
            buff[3] = (byte)(value >> 32);
            buff[4] = (byte)(value >> 24);
            buff[5] = (byte)(value >> 16);
            buff[6] = (byte)(value >> 8);
            buff[7] = (byte)(value);
            return buff;
        }

        public static byte[] GetBytes(uint value)
        {
            var buff = new byte[4];
            buff[0] = (byte)(value >> 24);
            buff[1] = (byte)(value >> 16);
            buff[2] = (byte)(value >> 8);
            buff[3] = (byte)(value);
            return buff;
        }

        public static byte[] GetBytes(ushort value)
        {
            var buff = new byte[2];
            buff[0] = (byte)(value >> 8);
            buff[1] = (byte)(value);
            return buff;
        }

        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            return (ushort)(
                (value[startIndex] << 8) |
                (value[startIndex + 1]));
        }

        public static uint ToUInt32(byte[] value, int startIndex)
        {
            return
                ((uint)value[startIndex] << 24) |
                ((uint)value[startIndex + 1] << 16) |
                ((uint)value[startIndex + 2] << 8) |
                value[startIndex + 3];
        }

        public static ulong ToUInt64(byte[] value, int startIndex)
        {
            return
                ((ulong)value[startIndex] << 56) |
                ((ulong)value[startIndex + 1] << 48) |
                ((ulong)value[startIndex + 2] << 40) |
                ((ulong)value[startIndex + 3] << 32) |
                ((ulong)value[startIndex + 4] << 24) |
                ((ulong)value[startIndex + 5] << 16) |
                ((ulong)value[startIndex + 6] << 8) |
                value[startIndex + 7];
        }
    }

    public static class BitConverterLE
    {
        public static byte[] GetBytes(ulong value)
        {
            var buff = new byte[8];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            buff[4] = (byte)(value >> 32);
            buff[5] = (byte)(value >> 40);
            buff[6] = (byte)(value >> 48);
            buff[7] = (byte)(value >> 56);
            return buff;
        }

        public static byte[] GetBytes(uint value)
        {
            var buff = new byte[4];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            return buff;
        }

        public static byte[] GetBytes(ushort value)
        {
            var buff = new byte[2];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            return buff;
        }
    }

    public static class General
    {
        public static void CopyBytes(byte[] dst, int dstOffset, byte[] src)
        {
            Buffer.BlockCopy(src, 0, dst, dstOffset, src.Length);
        }

        public static byte[] StringToAscii(string s)
        {
            var retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }
    }

    public static class OggCRC
    {
        private static readonly uint[] _lut = new uint[256];

        static OggCRC()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint x = i << 24;
                for (uint j = 0; j < 8; j++)
                {
                    x = ((x & 0x80000000U) != 0) ? ((x << 1) ^ 0x04C11DB7) : (x << 1);
                }
                _lut[i] = x;
            }
        }

        public static uint Calculate(byte[] buff, int offset, int length)
        {
            uint crc = 0;
            for (int i = 0; i < length; i++)
            {
                crc = _lut[((crc >> 24) ^ buff[offset + i]) & 0xFF] ^ (crc << 8);
            }
            return crc;
        }
    }
}