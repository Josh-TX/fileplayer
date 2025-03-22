using FilePlayer.Controllers;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

public class DownloadService
{
#if DEBUG
    private static string _ytdlpPath = Path.Combine(Directory.GetCurrentDirectory(), "binaries", "yt-dlp.exe");
    private static string _ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "binaries", "ffmpeg.exe");
#else
    private static string _ytdlpPath = "yt-dlp";
    private static string _ffmpegPath = "ffmpeg";
#endif

    //private string _binariesPath = Path.Combine(BasePathHelper.BasePath, "binaries");


    private List<string> _commonHeights = new List<string> { "240", "360", "480", "720", "1080" };

    public DownloadService()
    {
#if DEBUG
        DownloadBinaries().Wait();
#endif
    }

    public async Task<UrlInfo> GetInfo(string url)
    {
        var ytdl = new YoutubeDL();
        ytdl.YoutubeDLPath = _ytdlpPath;
        var fetchResult = await ytdl.RunVideoDataFetch(url);
        if (!fetchResult.Success)
        {
            throw new Exception("ytdl.RunVideoDataFetch errored: " + String.Join(" | ", fetchResult.ErrorOutput));
        }
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
    public async void StartDownload(string outputPath, string url, int? preferredHeight, string? overrideName, bool useMDate, bool compatCodec)
    {
        //intentionally async void.

        var ytdl = new YoutubeDL();
        ytdl.YoutubeDLPath = _ytdlpPath;
        ytdl.FFmpegPath = _ffmpegPath;
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
        options.NoMtime = !useMDate;
        RunResult<string> result;
        if (preferredHeight != null)
        {
            var baseFormat = $"bv*[height<={preferredHeight}]+ba / b[height<={preferredHeight}] / wv*+ba / w";
            options.Format = baseFormat;
            if (compatCodec)
            {
                options.Format = $"bv*[vcodec^=avc][height<={preferredHeight}]+ba[acodec^=mp4a] / " +  // Best split-track AVC within height limit
                 $"b[vcodec^=avc][height<={preferredHeight}] / " +  // Best combined-track AVC within height limit
                 $"bv*[vcodec^=avc]+ba[acodec^=mp4a] / " +  // Any split-track AVC (if no height-compliant one exists)
                 $"w[vcodec^=avc] / " +  // Any combined-track AVC (if no height-compliant one exists)
                 baseFormat; // no AVC video available at all, so fall back on non-AVC
            }
            result = await ytdl.RunVideoDownload(
                url,
                mergeFormat: DownloadMergeFormat.Mp4,
                overrideOptions: options
            );
        }
        else //must've wanted audio only
        {
            result = await ytdl.RunAudioDownload(
                url,
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
        }
        else
        {
            Directory.Move(tempOutputPath, tempOutputPath + "-ERROR");
        }
    }

    public static string GetFFmpegPath()
    {
        return _ffmpegPath;
    }

    private string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? "";
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        int count = 1;

        while (File.Exists(filePath))
        {
            count++;
            filePath = Path.Combine(directory, $"{fileNameWithoutExt} ({count}){extension}");
        }

        return filePath;
    }

#if DEBUG
    /// <summary>
    /// For debug mode only
    /// </summary>
    private async Task DownloadBinaries()
    {
        Directory.CreateDirectory("binaries");
        if (!File.Exists(_ytdlpPath))
        {
            await Utils.DownloadYtDlp(Path.GetDirectoryName(_ytdlpPath));
        }
        if (!File.Exists(_ffmpegPath))
        {
            await Utils.DownloadFFmpeg(Path.GetDirectoryName(_ffmpegPath));
        }
    }
#endif
}
