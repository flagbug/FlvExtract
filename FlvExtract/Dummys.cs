namespace FlvExtract
{
    internal class DummyAudioWriter : IAudioWriter
    {
        public string Path
        {
            get
            {
                return null;
            }
        }

        public void Finish()
        {
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
        }
    }

    internal class DummyVideoWriter : IVideoWriter
    {
        public string Path
        {
            get
            {
                return null;
            }
        }

        public void Finish(FractionUInt32 averageFrameRate)
        {
        }

        public void WriteChunk(byte[] chunk, uint timeStamp, int frameType)
        {
        }
    }
}