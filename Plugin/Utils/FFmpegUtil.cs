﻿using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GH_Timeline
{
    /// <summary>
    /// Utilities for working with FFMPEG
    /// </summary>
    internal static class FFmpegUtil
    {
        public const string DOWNLOAD_URL_WINDOWS = "https://github.com/GyanD/codexffmpeg/releases/download/6.0/ffmpeg-6.0-essentials_build.zip";
        public const string DOWNLOAD_URL_OSX = "https://evermeet.cx/ffmpeg/getrelease/zip";

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static string DownloadURL => IsWindows ? DOWNLOAD_URL_WINDOWS : DOWNLOAD_URL_OSX;
        private static string InstallFolder => Path.Combine(PluginInfo.WorkingFolder, "ffmpeg");
        private static string DownloadFileName => Path.Combine(PluginInfo.WorkingFolder, "ffmpeg-installer.zip");
        public static string ExecutablePath => IsWindows ? Path.Combine(InstallFolder, "ffmpeg-6.0-essentials_build", "bin", "ffmpeg.exe") : Path.Combine(InstallFolder, "ffmpeg");
        public static bool IsInstalled => File.Exists(ExecutablePath);

        /// <summary>
        /// Downloads and installs ffmpeg (if required) and communicates with the user via the Rhino command line
        /// </summary>
        /// <returns>True if the install was successful (or already existing)</returns>
        public static bool Install()
        {
            if (IsInstalled)
            {
                return true;
            }

            if (File.Exists(DownloadFileName))
            {
                File.Delete(DownloadFileName);
            }
            _ = Directory.CreateDirectory(Path.GetDirectoryName(DownloadFileName));

            try
            {
                RhinoApp.WriteLine("Installing ffmpeg...");

                using (WebClient wc = new WebClient())
                {
                    int downloadPercentage = 0;
                    wc.DownloadProgressChanged += (obj, arg) =>
                    {
                        downloadPercentage = arg.ProgressPercentage;
                    };

                    Exception error = null;

                    wc.DownloadFileCompleted += (obj, arg) =>
                    {
                        if (arg.Error != null)
                        {
                            error = arg.Error;
                        }
                        else if (arg.Cancelled)
                        {
                            error = new OperationCanceledException("Download cancelled.");
                        }
                    };
                    wc.DownloadFileAsync(new Uri(DownloadURL), DownloadFileName);

                    while (wc.IsBusy)
                    {
                        RhinoApp.CommandPrompt = $"Downloading ffmpeg... {downloadPercentage}%";
                        RhinoApp.Wait();
                        Application.DoEvents();
                        if (GH_Document.IsEscapeKeyDown())
                        {
                            wc.CancelAsync();
                            return false;
                        }
                    }

                    if (error is OperationCanceledException)
                    {
                        return false;
                    }

                    if (!File.Exists(DownloadFileName))
                    {
                        error = new FileNotFoundException("File was not found after download completed: " + DownloadFileName);
                    }

                    if (error != null)
                    {
                        RhinoApp.WriteLine("An exception occurred while downloading ffmpeg. Details: \n" + error);
                        return false;
                    }
                }


                RhinoApp.CommandPrompt = $"Extracting ffmpeg...";

                CancellationTokenSource zipCts = new CancellationTokenSource();
                Task zipTask = Task.Run(() =>
                {
                    if (Directory.Exists(InstallFolder))
                    {
                        Directory.Delete(InstallFolder, true);
                    }
                    _ = Directory.CreateDirectory(InstallFolder);

                    ZipFile.ExtractToDirectory(DownloadFileName, InstallFolder);
                });

                while (!zipTask.IsCompleted)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    RhinoApp.CommandPrompt = $"Extracting ffmpeg... " + sw.Elapsed;
                    RhinoApp.Wait();
                    Application.DoEvents();
                    if (GH_Document.IsEscapeKeyDown())
                    {
                        zipCts.Cancel();
                        zipCts.Dispose();
                        return false;
                    }
                }

                zipCts.Dispose();

                if (zipTask.IsCanceled)
                {
                    return false;
                }

                if (zipTask.IsFaulted)
                {
                    RhinoApp.WriteLine("An exception occurred while unzipping ffmpeg. Details\n" + zipTask.Exception);
                    return false;
                }

                if (!IsInstalled)
                {
                    RhinoApp.WriteLine($"Something went wrong when installing ffmpeg... File not found at {ExecutablePath}");
                    return false;
                }

                if (IsOSX)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "/bin/zsh",
                        Arguments = $"-c \"chmod 755 \'{ExecutablePath}\' \"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }).WaitForExit();
                }

                return true;

            }
            finally
            {
                RhinoApp.CommandPrompt = string.Empty;
            }
        }

        /// <summary>
        /// Use the template pattern to write video files sequentially, as 
        /// the user may attempt to create a new video in the same location while the old 
        /// one is still open.
        /// </summary>
        private static string CreateOutputFileName(string folder, string fileTemplate)
        {
            for (int i = 0; i < 10000; i++)
            {
                string name = "Animation_" + Regex.Replace(string.Format(fileTemplate, i), "\\.\\w*$", $".mp4");
                if (!File.Exists(Path.Combine(folder, name)))
                {
                    return name;
                }
            }

            throw new Exception($"Too many videos in path {folder}");
        }

        /// <summary>
        /// Runs ffmpeg via the command line to compile frames into a video frame.
        /// </summary>
        /// <param name="folder">The folder containing the video frames</param>
        /// <param name="fileTemplate">The template for the files. Must be compatible with <see cref="string.Format(string, object)"/>.</param>
        /// <param name="numFiles">The number of files</param>
        /// <param name="framerate">The framerate for putting together video frames</param>
        /// <param name="bitrate">The bitrate for the video x10000 i.e. 192 = 192k</param>
        /// <returns>The path to the compiled video.</returns>
        public static string Compile(string folder, string fileTemplate, int numFiles, int framerate, int bitrate)
        {
            if (!Install())
            {
                return null;
            }

            IEnumerable<string> EnumerateFiles()
            {
                for (int i = 0; i < numFiles; i++)
                {
                    yield return $"file \'{string.Format(fileTemplate, i)}\'";
                }
            }

            string outputFileName = CreateOutputFileName(folder, fileTemplate);
            string concatFileName = outputFileName.Replace("mp4", "txt");

            try
            {
                File.WriteAllLines(Path.Combine(folder, concatFileName), EnumerateFiles());

                Process proc = new Process();
                proc.StartInfo.FileName = ExecutablePath;
                proc.StartInfo.WorkingDirectory = folder;
                proc.StartInfo.Arguments = $"-f concat -r {framerate} -i \"{concatFileName}\" -vcodec libx264 -pix_fmt yuv420p -b:v {bitrate}k \"{outputFileName}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
#if DEBUG
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.ErrorDataReceived += (sendingProcess, errorLine) => RhinoApp.WriteLine(errorLine.Data);
                proc.OutputDataReceived += (sendingProcess, dataLine) => RhinoApp.WriteLine(dataLine.Data);
#endif

                if (proc.Start())
                {
#if DEBUG
                    proc.BeginErrorReadLine();
                    proc.BeginOutputReadLine();
#endif
                    try
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        while (!proc.HasExited)
                        {
                            RhinoApp.Wait();
                            Application.DoEvents();
                            RhinoApp.CommandPrompt = $"Encoding video... " + sw.Elapsed;
                            if (GH_Document.IsEscapeKeyDown())
                            {
                                proc.Kill();
                                return null;
                            }
                        }
                    }
                    finally
                    {
                        RhinoApp.CommandPrompt = string.Empty;
                    }

                    if (proc.ExitCode != 0)
                    {
                        RhinoApp.WriteLine($"Something went wrong while combining video frames... (ffmpeg quit with exit code {proc.ExitCode})");
                        return null;
                    }
                    else
                    {
                        string fullPath = Path.Combine(folder, outputFileName);
                        RhinoApp.WriteLine("Saved video: " + fullPath);
                        return fullPath;
                    }
                }
                return null;
            }
            finally
            {
                try
                {
                    File.Delete(Path.Combine(folder, concatFileName));
                }
                catch { }
            }
        }
    }
}
