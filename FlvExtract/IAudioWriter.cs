using System;

namespace FlvExtract
{
    internal interface IAudioWriter : IDisposable
    {
        void WriteChunk(byte[] chunk, uint timeStamp);
    }
}