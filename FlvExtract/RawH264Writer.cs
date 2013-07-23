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
    internal class RawH264Writer : IVideoWriter
    {
        private static readonly byte[] _startCode = { 0, 0, 0, 1 };

        private readonly Stream _fs;
        private int _nalLengthSize;

        public RawH264Writer(Stream outputStream)
        {
            _fs = outputStream;
        }

        public VideoFormat VideoFormat
        {
            get { return VideoFormat.H264; }
        }

        public void Finish(FractionUInt32 averageFrameRate)
        {
            _fs.Dispose();
        }

        public void WriteChunk(byte[] chunk, uint timeStamp, int frameType)
        {
            if (chunk.Length < 4) return;

            // Reference: decode_frame from libavcodec's h264.c

            if (chunk[0] == 0)
            { // Headers
                if (chunk.Length < 10) return;

                int offset, spsCount, ppsCount;

                offset = 8;
                _nalLengthSize = (chunk[offset++] & 0x03) + 1;
                spsCount = chunk[offset++] & 0x1F;
                ppsCount = -1;

                while (offset <= chunk.Length - 2)
                {
                    if ((spsCount == 0) && (ppsCount == -1))
                    {
                        ppsCount = chunk[offset++];
                        continue;
                    }

                    if (spsCount > 0) spsCount--;
                    else if (ppsCount > 0) ppsCount--;
                    else break;

                    int len = BitConverterBE.ToUInt16(chunk, offset);
                    offset += 2;
                    if (offset + len > chunk.Length) break;
                    _fs.Write(_startCode, 0, _startCode.Length);
                    _fs.Write(chunk, offset, len);
                    offset += len;
                }
            }
            else
            { // Video data
                int offset = 4;

                if (_nalLengthSize != 2)
                {
                    _nalLengthSize = 4;
                }

                while (offset <= chunk.Length - _nalLengthSize)
                {
                    int len = (_nalLengthSize == 2) ?
                        BitConverterBE.ToUInt16(chunk, offset) :
                        (int)BitConverterBE.ToUInt32(chunk, offset);
                    offset += _nalLengthSize;
                    if (offset + len > chunk.Length) break;
                    _fs.Write(_startCode, 0, _startCode.Length);
                    _fs.Write(chunk, offset, len);
                    offset += len;
                }
            }
        }
    }
}