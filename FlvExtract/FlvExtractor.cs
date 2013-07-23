using System;
using System.IO;

namespace FlvExtract
{
    public class FlvExtractor
    {
        public EventHandler<ProgressEventArgs> ProgressChanged;
        private FLVFile file;
        private Func<ExtractionInfo> infoFunc;

        private FlvExtractor()
        { }

        /// <summary>
        /// Creates a <see cref="FlvExtractor"/> that extracts only the audio track.
        /// </summary>
        /// <param name="inputStream">The input stream. This is your raw FLV data.</param>
        /// <param name="outputStream">The audio output stream. This is the stream that the audio track gets written to.</param>
        public static FlvExtractor CreateAudioExtractor(Stream inputStream, Stream outputStream)
        {
            var file = new FLVFile(inputStream, outputStream);

            return new FlvExtractor { file = file, infoFunc = () => new ExtractionInfo(file.AudioFormat) };
        }

        /// <summary>
        /// Creates a <see cref="FlvExtractor"/> that extracts the audio and video track.
        /// </summary>
        /// <param name="inputStream">The input stream. This is your raw FLV data.</param>
        /// <param name="audioOutputStream">The audio output stream. This is the stream that the audio track gets written to.</param>
        /// <param name="videoOutputStream">The video output stream. This is the stream that the video track gets written to.</param>
        public static FlvExtractor CreateExtractor(Stream inputStream, Stream audioOutputStream, Stream videoOutputStream)
        {
            var file = new FLVFile(inputStream, audioOutputStream, videoOutputStream);
            return new FlvExtractor { file = file, infoFunc = () => new ExtractionInfo(file.AudioFormat, file.VideoFormat) };
        }

        /// <summary>
        /// Creates a <see cref="FlvExtractor"/> that extracts only the video track.
        /// </summary>
        /// <param name="inputStream">The input stream. This is your raw FLV data.</param>
        /// <param name="outputStream">The video output stream. This is the stream that the video track gets written to.</param>
        public static FlvExtractor CreateVideoExtractor(Stream inputStream, Stream outputStream)
        {
            var file = new FLVFile(inputStream, videoOutputStream: outputStream);
            return new FlvExtractor { file = file, infoFunc = () => new ExtractionInfo(file.VideoFormat) };
        }

        /// <summary>
        /// Executes the audio extraction. This method runs synchronously but reports it's progress through the <see cref="ProgressChanged"/> event.
        /// </summary>
        /// <exception cref="ExtractionException">An error occured during the audio or video extraction.</exception>
        /// <exception cref="IOException">An error occured while writing to or reading from the disk.</exception>
        public ExtractionInfo Execute()
        {
            file.ProgressChanged += (sender, args) =>
            {
                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, args);
                }
            };

            try
            {
                file.ExtractStreams();
                return this.infoFunc();
            }

            finally
            {
                file.Dispose();
            }
        }
    }
}