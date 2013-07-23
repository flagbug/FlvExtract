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

using System;
using System.Collections.Generic;
using System.IO;

namespace FlvExtract
{
    internal class FLVFile : IDisposable
    {
        private readonly long _fileLength;
        private readonly List<string> _warnings;
        private readonly Stream audioOutputStream;
        private readonly Stream videoOutputStream;
        private IAudioWriter _audioWriter;
        private FractionUInt32? _averageFrameRate, _trueFrameRate;
        private long _fileOffset;
        private Stream _fs;
        private List<uint> _videoTimeStamps;
        private IVideoWriter _videoWriter;

        public FLVFile(Stream inputStream, Stream audioOutputStream = null, Stream videoOutputStream = null)
        {
            if (audioOutputStream == null && videoOutputStream == null)
                throw new ArgumentNullException();

            _warnings = new List<string>();
            _fs = inputStream;
            this.audioOutputStream = audioOutputStream;
            this.videoOutputStream = videoOutputStream;
            _fileOffset = 0;
            _fileLength = _fs.Length;
        }

        public AudioFormat AudioFormat { get; private set; }

        public FractionUInt32? AverageFrameRate
        {
            get { return _averageFrameRate; }
        }

        public FractionUInt32? TrueFrameRate
        {
            get { return _trueFrameRate; }
        }

        public VideoFormat VideoFormat { get; private set; }

        public string[] Warnings
        {
            get { return _warnings.ToArray(); }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_fs != null)
            {
                _fs.Dispose();
                _fs = null;
            }

            CloseOutput(null);
        }

        public void ExtractStreams()
        {
            uint dataOffset;

            _videoTimeStamps = new List<uint>();

            Seek(0);
            if (_fileLength < 4 || ReadUInt32() != 0x464C5601)
            {
                if (_fileLength >= 8 && ReadUInt32() == 0x66747970)
                {
                    throw new ExtractionException("This is a MP4 file. YAMB or MP4Box can be used to extract streams.");
                }

                throw new ExtractionException("This isn't a FLV file.");
            }

            ReadUInt8();
            dataOffset = ReadUInt32();

            Seek(dataOffset);

            ReadUInt32();
            while (_fileOffset < _fileLength)
            {
                if (!ReadTag()) break;
                if ((_fileLength - _fileOffset) < 4) break;
                ReadUInt32();
            }

            _averageFrameRate = CalculateAverageFrameRate();
            _trueFrameRate = CalculateTrueFrameRate();

            CloseOutput(_averageFrameRate);
        }

        private FractionUInt32? CalculateAverageFrameRate()
        {
            int frameCount = _videoTimeStamps.Count;

            if (frameCount > 1)
            {
                FractionUInt32 frameRate;
                frameRate.N = (uint)(frameCount - 1) * 1000;
                frameRate.D = _videoTimeStamps[frameCount - 1] - _videoTimeStamps[0];
                frameRate.Reduce();
                return frameRate;
            }

            return null;
        }

        private FractionUInt32? CalculateTrueFrameRate()
        {
            var deltaCount = new Dictionary<uint, uint>();
            int i, threshold;
            uint delta, count, minDelta;

            // Calculate the distance between the timestamps, count how many times each delta appears
            for (i = 1; i < _videoTimeStamps.Count; i++)
            {
                int deltaS = (int)((long)_videoTimeStamps[i] - _videoTimeStamps[i - 1]);

                if (deltaS <= 0) continue;
                delta = (uint)deltaS;

                if (deltaCount.ContainsKey(delta))
                {
                    deltaCount[delta] += 1;
                }
                else
                {
                    deltaCount.Add(delta, 1);
                }
            }

            threshold = _videoTimeStamps.Count / 10;
            minDelta = UInt32.MaxValue;

            // Find the smallest delta that made up at least 10% of the frames (grouping in delta+1
            // because of rounding, e.g. a NTSC video will have deltas of 33 and 34 ms)
            foreach (KeyValuePair<uint, uint> deltaItem in deltaCount)
            {
                delta = deltaItem.Key;
                count = deltaItem.Value;

                if (deltaCount.ContainsKey(delta + 1))
                {
                    count += deltaCount[delta + 1];
                }

                if ((count >= threshold) && (delta < minDelta))
                {
                    minDelta = delta;
                }
            }

            // Calculate the frame rate based on the smallest delta, and delta+1 if present
            if (minDelta != UInt32.MaxValue)
            {
                uint totalTime, totalFrames;

                count = deltaCount[minDelta];
                totalTime = minDelta * count;
                totalFrames = count;

                if (deltaCount.ContainsKey(minDelta + 1))
                {
                    count = deltaCount[minDelta + 1];
                    totalTime += (minDelta + 1) * count;
                    totalFrames += count;
                }

                if (totalTime != 0)
                {
                    FractionUInt32 frameRate;
                    frameRate.N = totalFrames * 1000;
                    frameRate.D = totalTime;
                    frameRate.Reduce();
                    return frameRate;
                }
            }

            // Unable to calculate frame rate
            return null;
        }

        private void CloseOutput(FractionUInt32? averageFrameRate)
        {
            if (_videoWriter != null)
            {
                _videoWriter.Finish(averageFrameRate ?? new FractionUInt32(25, 1));
                _videoWriter = null;
            }

            if (_audioWriter != null)
            {
                _audioWriter.Dispose();
                _audioWriter = null;
            }
        }

        private IAudioWriter GetAudioWriter(uint mediaInfo)
        {
            uint format = mediaInfo >> 4;
            uint rate = (mediaInfo >> 2) & 0x3;
            uint bits = (mediaInfo >> 1) & 0x1;
            uint chans = mediaInfo & 0x1;

            switch (format)
            {
                case 14:
                case 2:
                    return new MP3Writer(this.audioOutputStream, _warnings);

                case 3:
                case 0:
                    {
                        // PCM
                        int sampleRate = 0;
                        switch (rate)
                        {
                            case 0: sampleRate = 5512; break;
                            case 1: sampleRate = 11025; break;
                            case 2: sampleRate = 22050; break;
                            case 3: sampleRate = 44100; break;
                        }

                        if (format == 0)
                        {
                            _warnings.Add("PCM byte order unspecified, assuming little endian.");
                        }

                        return new WAVWriter(this.audioOutputStream, (bits == 1) ? 16 : 8,
                            (chans == 1) ? 2 : 1, sampleRate);
                    }

                case 10:
                    return new AACWriter(this.audioOutputStream);

                case 11:
                    return new SpeexWriter(this.audioOutputStream, (int)(_fileLength & 0xFFFFFFFF));
            }

            string typeStr;

            switch (format)
            {
                case 1:
                    typeStr = "ADPCM";
                    break;

                case 6:
                case 5:
                case 4:
                    typeStr = "Nellymoser";
                    break;

                default:
                    typeStr = "format=" + format;
                    break;
            }

            throw new ExtractionException("Unable to extract audio (" + typeStr + " is unsupported).");
        }

        private IVideoWriter GetVideoWriter(uint mediaInfo)
        {
            uint codecID = mediaInfo & 0x0F;

            switch (codecID)
            {
                case 5:
                case 4:
                case 2:
                    return new AVIWriter(this.videoOutputStream, (int)codecID, _warnings, false);

                case 7:
                    return new RawH264Writer(this.videoOutputStream);
            }

            string typeStr;

            if (codecID == 3)
                typeStr = "Screen";
            else if (codecID == 6)
                typeStr = "Screen2";
            else
                typeStr = "codecID=" + codecID;

            throw new ExtractionException("Unable to extract video (" + typeStr + " is unsupported).");
        }

        private byte[] ReadBytes(int length)
        {
            var buff = new byte[length];
            _fs.Read(buff, 0, length);
            _fileOffset += length;
            return buff;
        }

        private bool ReadTag()
        {
            uint tagType, dataSize, timeStamp, streamID, mediaInfo;

            if ((_fileLength - _fileOffset) < 11)
            {
                return false;
            }

            // Read tag header
            tagType = ReadUInt8();
            dataSize = ReadUInt24();
            timeStamp = ReadUInt24();
            timeStamp |= ReadUInt8() << 24;
            ReadUInt24();

            // Read tag data
            if (dataSize == 0)
            {
                return true;
            }
            if ((_fileLength - _fileOffset) < dataSize)
            {
                return false;
            }
            mediaInfo = ReadUInt8();
            dataSize -= 1;
            byte[] data = ReadBytes((int)dataSize);

            if (tagType == 0x8 && this.audioOutputStream != null)
            {
                // Audio
                if (_audioWriter == null)
                {
                    _audioWriter = this.GetAudioWriter(mediaInfo);
                    this.AudioFormat = this._audioWriter.AudioFormat;
                }

                _audioWriter.WriteChunk(data, timeStamp);
            }

            else if ((tagType == 0x9) && ((mediaInfo >> 4) != 5) && this.videoOutputStream != null)
            {
                // Video
                if (_videoWriter == null)
                {
                    _videoWriter = this.GetVideoWriter(mediaInfo);
                    this.VideoFormat = this._videoWriter.VideoFormat;
                }

                _videoTimeStamps.Add(timeStamp);
                _videoWriter.WriteChunk(data, timeStamp, (int)((mediaInfo & 0xF0) >> 4));
            }

            return true;
        }

        private uint ReadUInt24()
        {
            var x = new byte[4];
            _fs.Read(x, 1, 3);
            _fileOffset += 3;
            return BitConverterBE.ToUInt32(x, 0);
        }

        private uint ReadUInt32()
        {
            var x = new byte[4];
            _fs.Read(x, 0, 4);
            _fileOffset += 4;
            return BitConverterBE.ToUInt32(x, 0);
        }

        private uint ReadUInt8()
        {
            _fileOffset += 1;
            return (uint)_fs.ReadByte();
        }

        private void Seek(long offset)
        {
            _fs.Seek(offset, SeekOrigin.Begin);
            _fileOffset = offset;
        }
    }
}