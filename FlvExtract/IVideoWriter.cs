namespace FlvExtract
{
    internal interface IVideoWriter
    {
        VideoFormat VideoFormat { get; }

        void Finish(FractionUInt32 averageFrameRate);

        void WriteChunk(byte[] chunk, uint timeStamp, int frameType);
    }
}