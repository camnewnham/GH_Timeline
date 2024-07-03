using System;
using System.Collections.Generic;

namespace GH_Timeline
{

    internal abstract class FFmpegException : Exception
    {
        public FFmpegException(string message) : base(message) { }
    }

    /// <summary>
    /// Utilities for working with FFMPEG
    /// </summary>
    internal class NotInstalledException : FFmpegException
    {
        public NotInstalledException() : base("FFMpeg is not installed.")
        {
        }
    }

    internal class FFmpegExecutionException : FFmpegException
    {
        public readonly IEnumerable<string> StdOut;
        public readonly int ExitCode;

        public FFmpegExecutionException(int exitCode, IEnumerable<string> stdOut) : base("An exception occurred while executing ffmpeg via the command line.")
        {
            StdOut = stdOut;
            ExitCode = exitCode;
        }

        public override string ToString()
        {
            return $"FFmpeg exited with code {ExitCode}.\n" + string.Join("\n", StdOut);
        }
    }

    internal class FFmpegStartException : FFmpegException
    {
        public FFmpegStartException() : base("Failed to start ffmpeg.") { }
    }
}
