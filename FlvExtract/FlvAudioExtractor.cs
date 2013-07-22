using System;
using System.IO;

namespace FlvExtract
{
    public class FlvAudioExtractor
    {
        public EventHandler<ProgressEventArgs> ProgressChanged;
        private readonly Stream inputStream;
        private readonly Stream outputStream;

        /// <summary>
        /// Creates a new instance of the <see cref="FlvAudioExtractor"/> class with the specified input and output stream.
        /// </summary>
        /// <param name="inputStream">The input stream. This is your raw FLV data.</param>
        /// <param name="outputStream">The output stream. This is the stream that the audio track gets written to.</param>
        public FlvAudioExtractor(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");

            if (outputStream == null)
                throw new ArgumentNullException("outputStream");

            this.inputStream = inputStream;
            this.outputStream = outputStream;
        }

        /// <summary>
        /// Executes the audio extraction. This method runs synchronously but reports it's progress through the <see cref="ProgressChanged"/> event.
        /// </summary>
        /// <exception cref="AudioExtractionException">An error occured during audio extraction.</exception>
        /// <exception cref="IOException">An error occured while writing to or reading from the disk.</exception>
        public void Execute()
        {
            /*
            using (var flvFile = new FlvFile(this.inputStream, this.outputStream))
            {
                flvFile.ConversionProgressChanged += (sender, args) =>
                {
                    if (this.ProgressChanged != null)
                    {
                        this.ProgressChanged(this, new ProgressEventArgs(args.ProgressPercentage));
                    }
                };

                flvFile.ExtractStreams();
            }*/
        }
    }
}