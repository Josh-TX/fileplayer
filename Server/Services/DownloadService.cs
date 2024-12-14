using FilePlayer.Controllers;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

public class DownloadService
{
    private string _binariesPath = Path.Combine(BasePathHelper.BasePath, "binaries");
    private List<string> _commonHeights = new List<string> { "240", "360", "480", "720", "1080" };

    public DownloadService()
    {

    }

    public async Task<UrlInfo> GetInfo(string url)
    {
        var ytdl = new YoutubeDL();
        ytdl.YoutubeDLPath = Path.Combine(_binariesPath, GetYtDlpBinaryName());
        if (!File.Exists(ytdl.YoutubeDLPath))
        {
            await Utils.DownloadYtDlp();
        }
        var fetchResult = await ytdl.RunVideoDataFetch(url);
        var resolutions = fetchResult.Data.Formats.Select(z => z.Resolution).Distinct().ToList();
        var availableHeights = _commonHeights.Where(height => resolutions.Any(res => res.EndsWith("x" + height)));
        var hasVideo = resolutions.Any(z => z != "audio only");
        return new UrlInfo
        {
            Title = fetchResult.Data.Title,
            Duration = fetchResult.Data.Duration.HasValue ? Convert.ToInt32(fetchResult.Data.Duration.Value) : null,
            Heights = availableHeights,
            AudioOnly = !hasVideo,
            ThumbnailUrl = fetchResult.Data.Thumbnail,
            PublishDate = fetchResult.Data.UploadDate
        };
    }

    /// <summary>
    /// Downloads media via yt-dlp
    /// </summary>
    /// <param name="outputPath">The full path to the output directory</param>
    /// <param name="url">The url to download mediafrom</param>
    /// <param name="preferredHeight">When null, download just audio</param>
    /// <param name="overrideName">Output file name (should not include file extension)</param>
    public async void StartDownload(string outputPath, string url, int? preferredHeight, string? overrideName, bool useMDate)
    {
        //intentionally async void.

        if (!string.IsNullOrEmpty(_binariesPath))
        {
            Directory.CreateDirectory(_binariesPath);
        }
        var ytdl = new YoutubeDL();
        ytdl.YoutubeDLPath = Path.Combine(_binariesPath, GetYtDlpBinaryName());
        if (!File.Exists(ytdl.YoutubeDLPath))
        {
            await Utils.DownloadYtDlp();
        }
        ytdl.FFmpegPath = Path.Combine(_binariesPath, GetFfmpegBinaryName());
        if (!File.Exists(ytdl.FFmpegPath))
        {
            await Utils.DownloadFFmpeg();
        }

        var options = new OptionSet();
        var tempOutputPath = Path.Combine(outputPath, $"temp-download-folder-{DateTime.Now.ToString("HH-mm-ss")}");
        Directory.CreateDirectory(tempOutputPath);
        if (!string.IsNullOrEmpty(overrideName))
        {
            options.Output = Path.Combine(tempOutputPath, overrideName + ".%(ext)s");
        }
        else
        {
            options.Output = Path.Combine(tempOutputPath, "%(title)s.%(ext)s");
        }

        //var progress = new Progress<DownloadProgress>(p =>
        //{
        //    if (p.State == DownloadState.Downloading)
        //    {
        //        Console.WriteLine($"Download in progress ({Math.Round(p.Progress * 1000) / 10}%) from " + url);
        //    }
        //});

        options.NoMtime = !useMDate;
        RunResult<string> result;
        if (preferredHeight != null)
        {
            options.Format = $"bv*[height<={preferredHeight}]+ba / b[height<={preferredHeight}] / wv*+ba / w";
            result = await ytdl.RunVideoDownload(
                url,
                //progress: progress,
                mergeFormat: DownloadMergeFormat.Mp4,
                overrideOptions: options
            );
        }
        else //must've wanted audio only
        {
            result = await ytdl.RunAudioDownload(
                url,
                //progress: progress,
                AudioConversionFormat.Mp3,
                overrideOptions: options
            );
        }
        if (result.Success)
        {
            var filename = Path.GetFileName(result.Data);
            var newPath = GetUniqueFilePath(Path.Combine(outputPath, filename));
            var oldPath = Path.Combine(tempOutputPath, filename);
            File.Move(oldPath, newPath);
            Directory.Delete(tempOutputPath, recursive: true);
        } else
        {
            Directory.Move(tempOutputPath, tempOutputPath + "-ERROR");
        }
    }

    private string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? "";
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        int count = 0;

        while (File.Exists(filePath))
        {
            count++;
            filePath = Path.Combine(directory, $"{fileNameWithoutExt} ({count}){extension}");
        }

        return filePath;
    }


    private static string GetYtDlpBinaryName()
    {
#if DEBUG
        return "yt-dlp.exe";
#else
        return "yt-dlp";
#endif
    }

    private static string GetFfmpegBinaryName()
    {
#if DEBUG
        return "ffmpeg.exe";
#else
        return "ffmpeg";
#endif
    }
}
