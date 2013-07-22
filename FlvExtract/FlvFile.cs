using System;
using System.IO;

namespace FlvExtract
{
    internal class FlvFile : IDisposable
    {
        private readonly Stream inputStream;
        private readonly Stream outputStream;
        private IAudioExtractor audioExtractor;
        private long fileOffset;

        public FlvFile(Stream inputStream, Stream outputStream)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
            this.fileOffset = 0;
        }

        public event EventHandler<ProgressEventArgs> ConversionProgressChanged;

        public bool ExtractedAudio { get; private set; }

        public void Dispose()
        {
            if (this.inputStream != null)
            {
                this.inputStream.Dispose();
            }

            this.CloseOutput();
        }

        public void ExtractStreams()
        {
            this.Seek(0);

            if (this.ReadUInt32() != 0x464C5601)
            {
                // not a FLV file
                throw new AudioExtractionException("Invalid input file. Impossible to extract audio track.");
            }

            this.ReadUInt8();
            uint dataOffset = this.ReadUInt32();

            this.Seek(dataOffset);

            this.ReadUInt32();

            while (fileOffset < this.inputStream.Length)
            {
                if (!ReadTag())
                {
                    break;
                }

                if (this.inputStream.Length - fileOffset < 4)
                {
                    break;
                }

                this.ReadUInt32();

                double progress = (this.fileOffset * 1.0 / this.inputStream.Length) * 100;

                if (this.ConversionProgressChanged != null)
                {
                    this.ConversionProgressChanged(this, new ProgressEventArgs(progress));
                }
            }

            this.CloseOutput();
        }

        private void CloseOutput()
        {
            if (this.audioExtractor != null)
            {
                this.audioExtractor.Dispose();
                this.audioExtractor = null;
            }
        }

        private IAudioExtractor GetAudioWriter(uint mediaInfo)
        {
            uint format = mediaInfo >> 4;

            switch (format)
            {
                case 14:
                case 2:
                    return new Mp3AudioExtractor(this.outputStream);

                case 10:
                    return new AacAudioExtractor(this.outputStream);
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

            throw new AudioExtractionException("Unable to extract audio (" + typeStr + " is unsupported).");
        }

        private byte[] ReadBytes(int length)
        {
            var buff = new byte[length];

            this.inputStream.Read(buff, 0, length);
            this.fileOffset += length;

            return buff;
        }

        private bool ReadTag()
        {
            if (this.inputStream.Length - this.fileOffset < 11)
                return false;

            // Read tag header
            uint tagType = ReadUInt8();
            uint dataSize = ReadUInt24();
            uint timeStamp = ReadUInt24();
            timeStamp |= this.ReadUInt8() << 24;
            this.ReadUInt24();

            // Read tag data
            if (dataSize == 0)
                return true;

            if (this.inputStream.Length - this.fileOffset < dataSize)
                return false;

            uint mediaInfo = this.ReadUInt8();
            dataSize -= 1;
            byte[] data = this.ReadBytes((int)dataSize);

            if (tagType == 0x8)
            {
                // If we have no audio writer, create one
                if (this.audioExtractor == null)
                {
                    this.audioExtractor = this.GetAudioWriter(mediaInfo);
                    this.ExtractedAudio = this.audioExtractor != null;
                }

                if (this.audioExtractor == null)
                {
                    throw new InvalidOperationException("No supported audio writer found.");
                }

                this.audioExtractor.WriteChunk(data, timeStamp);
            }

            return true;
        }

        private uint ReadUInt24()
        {
            var x = new byte[4];

            this.inputStream.Read(x, 1, 3);
            this.fileOffset += 3;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt32()
        {
            var x = new byte[4];

            this.inputStream.Read(x, 0, 4);
            this.fileOffset += 4;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt8()
        {
            this.fileOffset += 1;
            return (uint)this.inputStream.ReadByte();
        }

        private void Seek(long offset)
        {
            this.inputStream.Seek(offset, SeekOrigin.Begin);
            this.fileOffset = offset;
        }
    }
}