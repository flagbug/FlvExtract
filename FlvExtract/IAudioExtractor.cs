using System;

namespace FlvExtract
{
    internal interface IAudioExtractor : IDisposable
    {
        /// <exception cref="AudioExtractionException">An error occured while writing the chunk.</exception>
        void WriteChunk(byte[] chunk, uint timeStamp);
    }
}