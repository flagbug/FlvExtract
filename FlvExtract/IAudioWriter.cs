using System;

namespace FlvExtract
{
    internal interface IAudioWriter : IDisposable
    {
        AudioFormat AudioFormat { get; }

        void WriteChunk(byte[] chunk, uint timeStamp);
    }
}