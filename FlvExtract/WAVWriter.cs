// FLV Extract
// Copyright (C) 2006-2012  J.D. Purcell (moitah@yahoo.com)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System.IO;

namespace FlvExtract
{
    internal class WAVWriter : IAudioWriter
    {
        private WAVWriter2 _wr;
        private int blockAlign;

        public WAVWriter(Stream outputStream, int bitsPerSample, int channelCount, int sampleRate)
        {
            _wr = new WAVWriter2(outputStream, bitsPerSample, channelCount, sampleRate);
            blockAlign = (bitsPerSample / 8) * channelCount;
        }

        public AudioFormat AudioFormat
        {
            get { return AudioFormat.Wav; }
        }

        public void Dispose()
        {
            _wr.Close();
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
            _wr.Write(chunk, chunk.Length / blockAlign);
        }
    }

    // Note: Why does this even exist?
    internal class WAVWriter2
    {
        private int _bitsPerSample, _channelCount, _sampleRate, _blockAlign;
        private BinaryWriter _bw;
        private bool _canSeek;
        private long _finalSampleLen, _sampleLen;
        private bool _wroteHeaders;

        public WAVWriter2(Stream stream, int bitsPerSample, int channelCount, int sampleRate)
        {
            _bitsPerSample = bitsPerSample;
            _channelCount = channelCount;
            _sampleRate = sampleRate;
            _blockAlign = _channelCount * ((_bitsPerSample + 7) / 8);

            _bw = new BinaryWriter(stream);
            _canSeek = stream.CanSeek;
        }

        public long FinalSampleCount
        {
            set
            {
                _finalSampleLen = value;
            }
        }

        public long Position
        {
            get
            {
                return _sampleLen;
            }
        }

        public void Close()
        {
            if (((_sampleLen * _blockAlign) & 1) == 1)
            {
                _bw.Write((byte)0);
            }

            try
            {
                if (_sampleLen != _finalSampleLen)
                {
                    if (_canSeek)
                    {
                        uint dataChunkSize = GetDataChunkSize(_sampleLen);
                        _bw.Seek(4, SeekOrigin.Begin);
                        _bw.Write((uint)(dataChunkSize + (dataChunkSize & 1) + 36));
                        _bw.Seek(40, SeekOrigin.Begin);
                        _bw.Write((uint)dataChunkSize);
                    }
                    else
                    {
                        throw new ExtractionException("Samples written differs from the expected sample count.");
                    }
                }
            }
            finally
            {
                _bw.Dispose();
                _bw = null;
            }
        }

        public void Write(byte[] buff, int sampleCount)
        {
            if (sampleCount <= 0) return;

            if (!_wroteHeaders)
            {
                WriteHeaders();
                _wroteHeaders = true;
            }

            _bw.Write(buff, 0, sampleCount * _blockAlign);
            _sampleLen += sampleCount;
        }

        private uint GetDataChunkSize(long sampleCount)
        {
            const long maxFileSize = 0x7FFFFFFEL;
            long dataSize = sampleCount * _blockAlign;
            if ((dataSize + 44) > maxFileSize)
            {
                dataSize = ((maxFileSize - 44) / _blockAlign) * _blockAlign;
            }
            return (uint)dataSize;
        }

        private void WriteHeaders()
        {
            const uint fccRIFF = 0x46464952;
            const uint fccWAVE = 0x45564157;
            const uint fccFormat = 0x20746D66;
            const uint fccData = 0x61746164;

            uint dataChunkSize = GetDataChunkSize(_finalSampleLen);

            _bw.Write(fccRIFF);
            _bw.Write((uint)(dataChunkSize + (dataChunkSize & 1) + 36));
            _bw.Write(fccWAVE);

            _bw.Write(fccFormat);
            _bw.Write((uint)16);
            _bw.Write((ushort)1);
            _bw.Write((ushort)_channelCount);
            _bw.Write((uint)_sampleRate);
            _bw.Write((uint)(_sampleRate * _blockAlign));
            _bw.Write((ushort)_blockAlign);
            _bw.Write((ushort)_bitsPerSample);

            _bw.Write(fccData);
            _bw.Write((uint)dataChunkSize);
        }
    }
}