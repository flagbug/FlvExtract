namespace FlvExtract
{
    internal interface IAudioWriter
    {
        string Path { get; }

        void Finish();

        void WriteChunk(byte[] chunk, uint timeStamp);
    }
}