using System;

namespace FlvExtract
{
    public enum AudioFormat
    {
        Mp3,
        Aac,
        Speex,
        Wav
    }

    public enum VideoFormat
    {
        H264,
        Avi
    }

    /// <summary>
    /// This class represents information about the successful extraction of a audio and/or video stream.
    /// </summary>
    public class ExtractionInfo
    {
        internal ExtractionInfo(AudioFormat audioFormat, VideoFormat videoFormat)
        {
            this.AudioFormat = audioFormat;
            this.VideoFormat = videoFormat;
        }

        internal ExtractionInfo(AudioFormat audioFormat)
        {
            this.AudioFormat = audioFormat;
        }

        internal ExtractionInfo(VideoFormat videoFormat)
        {
            this.VideoFormat = videoFormat;
        }

        /// <summary>
        /// Gets the file-system extension that the audio format has. e.g ".mp3".
        /// </summary>
        public string AudioExtension
        {
            get
            {
                switch (this.AudioFormat)
                {
                    case FlvExtract.AudioFormat.Aac:
                        return ".aac";

                    case FlvExtract.AudioFormat.Mp3:
                        return ".mp3";

                    case FlvExtract.AudioFormat.Speex:
                        return ".spx";

                    case FlvExtract.AudioFormat.Wav:
                        return ".wav";
                }

                throw new NotImplementedException("Something is wrong here");
            }
        }

        public AudioFormat? AudioFormat { get; private set; }

        /// <summary>
        /// Gets the file-system extension that the video format has. e.g ".avi".
        /// </summary>
        public string VideoExtension
        {
            get
            {
                switch (this.VideoFormat)
                {
                    case FlvExtract.VideoFormat.Avi:
                        return ".avi";

                    case FlvExtract.VideoFormat.H264:
                        return ".mp4";
                }

                throw new NotImplementedException("Something is wrong here");
            }
        }

        public VideoFormat? VideoFormat { get; private set; }
    }
}