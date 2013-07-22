namespace FlvExtract
{
    internal interface IVideoWriter
    {
        void Finish(FractionUInt32 averageFrameRate);

        void WriteChunk(byte[] chunk, uint timeStamp, int frameType);
    }
}