using System.Collections.Generic;
using System.IO;

namespace FlvExtract
{
    internal class MP3Writer : IAudioWriter
    {
        private readonly List<byte[]> _chunkBuffer;
        private readonly List<uint> _frameOffsets;
        private readonly Stream _fs;
        private readonly List<string> _warnings;
        private int _channelMode;
        private bool _delayWrite;
        private int _firstBitRate;
        private uint _firstFrameHeader;
        private bool _hasVBRHeader;
        private bool _isVBR;
        private int _mpegVersion;
        private int _sampleRate;
        private uint _totalFrameLength;
        private bool _writeVBRHeader;

        public MP3Writer(Stream outputStream, List<string> warnings)
        {
            _fs = outputStream;
            _warnings = warnings;
            _chunkBuffer = new List<byte[]>();
            _frameOffsets = new List<uint>();
            _delayWrite = true;
        }

        public void Dispose()
        {
            Flush();

            if (_writeVBRHeader)
            {
                _fs.Seek(0, SeekOrigin.Begin);
                WriteVBRHeader(false);
            }

            _fs.Dispose();
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
            _chunkBuffer.Add(chunk);
            ParseMP3Frames(chunk);
            if (_delayWrite && _totalFrameLength >= 65536)
            {
                _delayWrite = false;
            }
            if (!_delayWrite)
            {
                Flush();
            }
        }

        private void Flush()
        {
            foreach (byte[] chunk in _chunkBuffer)
            {
                _fs.Write(chunk, 0, chunk.Length);
            }
            _chunkBuffer.Clear();
        }

        private int GetFrameDataOffset(int mpegVersion, int channelMode)
        {
            return 4 + ((mpegVersion == 3) ?
                ((channelMode == 3) ? 17 : 32) :
                ((channelMode == 3) ? 9 : 17));
        }

        private int GetFrameLength(int mpegVersion, int bitRate, int sampleRate, int padding)
        {
            return ((mpegVersion == 3) ? 144 : 72) * bitRate / sampleRate + padding;
        }

        private void ParseMP3Frames(byte[] buff)
        {
            int[] MPEG1BitRate = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
            int[] MPEG2XBitRate = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
            int[] MPEG1SampleRate = { 44100, 48000, 32000, 0 };
            int[] MPEG20SampleRate = { 22050, 24000, 16000, 0 };
            int[] MPEG25SampleRate = { 11025, 12000, 8000, 0 };

            int offset = 0;
            int length = buff.Length;

            while (length >= 4)
            {
                ulong header;
                int mpegVersion, layer, bitRate, sampleRate, padding, channelMode;
                int frameLen;

                header = (ulong)BitConverterBE.ToUInt32(buff, offset) << 32;
                if (BitHelper.Read(ref header, 11) != 0x7FF)
                {
                    break;
                }
                mpegVersion = BitHelper.Read(ref header, 2);
                layer = BitHelper.Read(ref header, 2);
                BitHelper.Read(ref header, 1);
                bitRate = BitHelper.Read(ref header, 4);
                sampleRate = BitHelper.Read(ref header, 2);
                padding = BitHelper.Read(ref header, 1);
                BitHelper.Read(ref header, 1);
                channelMode = BitHelper.Read(ref header, 2);

                if ((mpegVersion == 1) || (layer != 1) || (bitRate == 0) || (bitRate == 15) || (sampleRate == 3))
                {
                    break;
                }

                bitRate = ((mpegVersion == 3) ? MPEG1BitRate[bitRate] : MPEG2XBitRate[bitRate]) * 1000;

                if (mpegVersion == 3)
                    sampleRate = MPEG1SampleRate[sampleRate];
                else if (mpegVersion == 2)
                    sampleRate = MPEG20SampleRate[sampleRate];
                else
                    sampleRate = MPEG25SampleRate[sampleRate];

                frameLen = GetFrameLength(mpegVersion, bitRate, sampleRate, padding);
                if (frameLen > length)
                {
                    break;
                }

                bool isVBRHeaderFrame = false;
                if (_frameOffsets.Count == 0)
                {
                    // Check for an existing VBR header just to be safe (I haven't seen any in FLVs)
                    int o = offset + GetFrameDataOffset(mpegVersion, channelMode);
                    if (BitConverterBE.ToUInt32(buff, o) == 0x58696E67)
                    { // "Xing"
                        isVBRHeaderFrame = true;
                        _delayWrite = false;
                        _hasVBRHeader = true;
                    }
                }

                if (!isVBRHeaderFrame)
                {
                    if (_firstBitRate == 0)
                    {
                        _firstBitRate = bitRate;
                        _mpegVersion = mpegVersion;
                        _sampleRate = sampleRate;
                        _channelMode = channelMode;
                        _firstFrameHeader = BitConverterBE.ToUInt32(buff, offset);
                    }
                    else if (!_isVBR && (bitRate != _firstBitRate))
                    {
                        _isVBR = true;
                        if (_hasVBRHeader)
                        {
                        }
                        else if (_delayWrite)
                        {
                            WriteVBRHeader(true);
                            _writeVBRHeader = true;
                            _delayWrite = false;
                        }
                        else
                        {
                            _warnings.Add("Detected VBR too late, cannot add VBR header.");
                        }
                    }
                }

                _frameOffsets.Add(_totalFrameLength + (uint)offset);

                offset += frameLen;
                length -= frameLen;
            }

            _totalFrameLength += (uint)buff.Length;
        }

        private void WriteVBRHeader(bool isPlaceholder)
        {
            var buff = new byte[GetFrameLength(_mpegVersion, 64000, _sampleRate, 0)];
            if (!isPlaceholder)
            {
                uint header = _firstFrameHeader;
                int dataOffset = GetFrameDataOffset(_mpegVersion, _channelMode);
                header &= 0xFFFF0DFF; // Clear bitrate and padding fields
                header |= 0x00010000; // Set protection bit (indicates that CRC is NOT present)
                header |= (uint)((_mpegVersion == 3) ? 5 : 8) << 12; // 64 kbit/sec
                General.CopyBytes(buff, 0, BitConverterBE.GetBytes(header));
                General.CopyBytes(buff, dataOffset, BitConverterBE.GetBytes(0x58696E67)); // "Xing"
                General.CopyBytes(buff, dataOffset + 4, BitConverterBE.GetBytes((uint)0x7)); // Flags
                General.CopyBytes(buff, dataOffset + 8, BitConverterBE.GetBytes((uint)_frameOffsets.Count)); // Frame count
                General.CopyBytes(buff, dataOffset + 12, BitConverterBE.GetBytes(_totalFrameLength)); // File length
                for (int i = 0; i < 100; i++)
                {
                    int frameIndex = (int)((i / 100.0) * _frameOffsets.Count);
                    buff[dataOffset + 16 + i] = (byte)((_frameOffsets[frameIndex] / (double)_totalFrameLength) * 256.0);
                }
            }

            _fs.Write(buff, 0, buff.Length);
        }
    }
}