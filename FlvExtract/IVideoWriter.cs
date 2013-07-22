namespace FlvExtract
{
    internal interface IVideoWriter
    {
        string Path { get; }

        void Finish(FractionUInt32 averageFrameRate);

        void WriteChunk(byte[] chunk, uint timeStamp, int frameType);
    }
}