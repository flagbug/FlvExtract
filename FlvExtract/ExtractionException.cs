using System;

namespace FlvExtract
{
    public class ExtractionException : Exception
    {
        public ExtractionException(string message)
            : base(message)
        { }
    }
}