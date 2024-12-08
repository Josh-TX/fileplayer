using Microsoft.AspNetCore.Mvc;
using Murmur;
using System.Security.Cryptography;

namespace FilePlayer.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private DurationService _durationService;
        private ProgressService _progressService;
        private SettingsService _settingsService;
        private string[] _mediaExtensions = new[]{
            "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "3gp", "m4v", "ogv", "mpeg", "mpg", "f4v", "rmvb", "asf", "vob", "mxf", "divx",
            "mp3", "wav", "aac", "flac", "ogg", "alac", "m4a", "opus", "mid", "midi"
        };

        public ApiController(
            DurationService durationService,
            ProgressService progressService,
            SettingsService settingsService
            ) {
            _durationService = durationService;
            _progressService = progressService;
            _settingsService = settingsService;
        }

        private readonly string _dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        private HashAlgorithm _murmur128 = MurmurHash.Create128(managed: false);
        private IDictionary<(Guid, long), (DateOnly, double)> _progressData = new SortedList<(Guid, long), (DateOnly, double)>();

        [HttpGet]
        [Route("dir-contents")]
        public ActionResult<DirContentsResponse> GetDirContents([FromQuery] string path = "")
        {
            string fullDirectory = Path.Combine(_dataFolderPath, path);
            if (!Directory.Exists(fullDirectory))
            {
                return NotFound(new { Message = "Directory not found." });
            }
            var folderInfos = Directory.GetDirectories(fullDirectory).Select(directory =>
            {
                var tuple = GetFolderSizeAndFileCount(directory);
                return new FolderInfo
                {
                    FolderName = Path.GetFileName(directory),
                    MediaFileCount = tuple.MediaFileCount,
                    MediaDiskSize = tuple.MediaDiskSize,
                    ModifyDate = new FileInfo(directory).LastWriteTime,
                };
            }).ToList();
            var fileNames = Directory.GetFiles(fullDirectory).Select(f => Path.GetFileName(f)).ToList();
            var mediaFilenames = fileNames.Where(filename => _mediaExtensions.Any(ext => filename.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)));
            var mediaInfos = mediaFilenames.Select(mediaFilename =>
            {
                var fileInfo = new FileInfo(Path.Combine(fullDirectory, mediaFilename));
                var size = fileInfo.Length;
                var fileId = FileIdHelper.GetId(mediaFilename, size);
                //Calculating the duration is very time consuming (roughly 100ms per file), so here we only include it if it's cached
                var duration = _durationService.TryGetCachedDuration(fileId);
                var progressTuple = _progressService.GetProgressTuple(fileId);
                return new MediaInfo
                {
                    FileName = mediaFilename,
                    Duration = duration,
                    FileSize = size,
                    ModifyDate = fileInfo.LastWriteTime,
                    Progress = progressTuple?.Item1,
                    ProgressDate = progressTuple?.Item2,
                };
            }); 

            var response = new DirContentsResponse
            {
                FolderInfos = folderInfos,
                MediaInfos = mediaInfos
            };
            return Ok(response);
        }

        [HttpGet]
        [Route("dir-contents/durations")]
        public async Task<ActionResult<GetDurationsResponse>> GetDurations([FromQuery] string path = "")
        {
            string fullDirectory = Path.Combine(_dataFolderPath, path);
            if (!Directory.Exists(fullDirectory))
            {
                return NotFound(new { Message = "Directory not found." });
            }
            var directories = Directory.GetDirectories(fullDirectory).Select(d => Path.GetFileName(d)).ToList();
            var fileNames = Directory.GetFiles(fullDirectory).Select(f => Path.GetFileName(f)).ToList();
            var mediaFilenames = fileNames.Where(filename => _mediaExtensions.Any(ext => filename.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)));

            var taskRunner = new TaskPoolRunner(2);
            var durations = await taskRunner.Run(mediaFilenames, async fileName =>
            {
                var fullPath = Path.Combine(fullDirectory, fileName);
                var size = new FileInfo(fullPath).Length;
                var fileId = FileIdHelper.GetId(fileName, size);
                int? duration;
                try
                {
                    duration = await _durationService.CalculateDuration(fileId, fullPath);
                } catch (Exception)
                {
                    //sometimes there's a failure when reading concurrently
                    duration = await _durationService.CalculateDuration(fileId, fullPath);
                }
                if (duration != null)
                {
                    return new MediaDuration
                    {
                        FileName = fileName,
                        Duration = duration.Value
                    };
                }
                else
                {
                    //there was an error getting the duration. Maybe the file isn't actually a media file despite the extension
                    return null;
                }
            });
            return new GetDurationsResponse
            {
                Path = fullDirectory,
                MediaDurations = durations.Where(z => z != null)!,
            };
        }

        [HttpGet]
        [Route("media-info")]
        public async Task<ActionResult<MediaInfo>> GetMediaInfo([FromQuery] string path = "")
        {
            if (!_mediaExtensions.Any(ext => path.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { Message = "not a media file" });
            }
            var fullPath = Path.Combine(_dataFolderPath, path);
            var fileInfo = new FileInfo(fullPath);
            var fileName = Path.GetFileName(path);
            if (!fileInfo.Exists)
            {
                return NotFound(new { Message = "file not found." });
            }
            var size = fileInfo.Length;
            var fileId = FileIdHelper.GetId(fileName, size);
            var duration = await _durationService.CalculateDuration(fileId, fullPath);
            var progressTuple = _progressService.GetProgressTuple(fileId);
            var mediaInfo = new MediaInfo
            {
                FileName = fileName,
                Duration = duration,
                FileSize = size,
                ModifyDate = fileInfo.LastWriteTime,
                Progress = progressTuple?.Item1,
                ProgressDate = progressTuple?.Item2,
            };
            return Ok(mediaInfo);
        }

        [HttpPost]
        [Route("update-progress")]
        public ActionResult UpdateProgress([FromQuery] double progress, [FromQuery] string path = "")
        {
            if (!_mediaExtensions.Any(ext => path.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { Message = "not a media file" });
            }
            var fullPath = Path.Combine(_dataFolderPath, path);
            var fileInfo = new FileInfo(fullPath);
            var fileName = Path.GetFileName(path);
            if (!fileInfo.Exists)
            {
                return NotFound(new { Message = "file not found." });
            }
            var size = fileInfo.Length;
            var fileId = FileIdHelper.GetId(fileName, size);
            _progressService.SetProgress(fileId, Convert.ToSingle(progress));
            return Ok();
        }

        [HttpGet]
        [Route("settings")]
        public ActionResult GetSettings()
        {
            return Ok(_settingsService.GetSettings());
        }

        [HttpPost]
        [Route("update-settings")]
        public ActionResult UpdateSettings([FromBody] Settings settings)
        {
            _settingsService.UpdateSettings(settings);
            return Ok();
        }

        private (long MediaDiskSize, int MediaFileCount) GetFolderSizeAndFileCount(string folderPath)
        {
            long totalSize = 0;
            int mediaDiskSize = 0;
            var dirInfo = new DirectoryInfo(folderPath);
            if (!dirInfo.Exists)
            {
                throw new DirectoryNotFoundException($"The directory {folderPath} does not exist.");
            }
            foreach (var file in dirInfo.GetFiles())
            {
                if (_mediaExtensions.Any(ext => file.Name.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)))
                {
                    totalSize += file.Length;
                    mediaDiskSize++;
                }
            }
            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                var subDirResult = GetFolderSizeAndFileCount(subDirInfo.FullName);
                totalSize += subDirResult.MediaDiskSize;
                mediaDiskSize += subDirResult.MediaFileCount;
            }
            return (totalSize, mediaDiskSize);
        }

    }

    public class DirContentsResponse
    {
        public required IEnumerable<FolderInfo> FolderInfos { get; set; }
        public required IEnumerable<MediaInfo> MediaInfos { get; set; }
    }


    public class FolderInfo
    {
        public required string FolderName { get; set; }
        public long MediaDiskSize { get; set; }
        public int MediaFileCount { get; set; }
        public DateTime ModifyDate { get; set; }
    }

    public class MediaInfo
    {
        public required string FileName { get; set; }
        public long FileSize { get; set; }
        public int? Duration { get; set; }
        public DateTime ModifyDate { get; set; }
        public DateOnly? ProgressDate { get; set; }
        public float? Progress { get; set; }
    }

    public class GetDurationsResponse
    {
        public required string Path { get; set; }
        public required IEnumerable<MediaDuration> MediaDurations { get; set; }
    }

    public class MediaDuration
    {
        public required string FileName { get; set; }
        public int Duration { get; set; }
    }
}
