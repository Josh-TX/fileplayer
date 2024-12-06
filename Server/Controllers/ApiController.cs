using Microsoft.AspNetCore.Mvc;
using Murmur;
using System.Security.Cryptography;
using System.Text;

namespace FilePlayer.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private DurationService _durationService;
        private ProgressService _progressService;
        private string[] _mediaExtensions = new[]{
            "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "3gp", "m4v", "ogv", "mpeg", "mpg", "f4v", "rmvb", "asf", "vob", "mxf", "divx",
            "mp3", "wav", "aac", "flac", "ogg", "alac", "m4a", "opus", "mid", "midi"
        };

    public ApiController(
            DurationService durationService,
            ProgressService progressService) {
            _durationService = durationService;
            _progressService = progressService;
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
            var directories = Directory.GetDirectories(fullDirectory).Select(d => Path.GetFileName(d)).ToList();
            var fileNames = Directory.GetFiles(fullDirectory).Select(f => Path.GetFileName(f)).ToList();
            var mediaFilenames = fileNames.Where(filename => _mediaExtensions.Any(ext => filename.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)));
            var mediaInfos = mediaFilenames.Select(mediaFilename =>
            {
                var size = new System.IO.FileInfo(Path.Combine(fullDirectory, mediaFilename)).Length;
                var fileId = FileIdHelper.GetId(mediaFilename, size);
                var duration = _durationService.TryGetCachedDuration(fileId);
                var progressTuple = _progressService.GetProgressTuple(fileId);
                return new MediaInfo
                {
                    FileName = mediaFilename,
                    Duration = duration,
                    FileSize = size,
                    Progress = progressTuple.Item1,
                    LastDate = progressTuple.Item2,
                };
            });

            var response = new DirContentsResponse
            {
                Directories = directories,
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
                var size = new System.IO.FileInfo(fullPath).Length;
                var fileId = FileIdHelper.GetId(fileName, size);
                var duration = await _durationService.CalculateDuration(fileId, fullPath);
                return new MediaDuration
                {
                    FileName = fileName,
                    Duration = duration
                };
            });
            return new GetDurationsResponse {
                Path = fullDirectory,
                MediaDurations = durations,
            };
        }
    }

    public class DirContentsResponse
    {
        public required IEnumerable<string> Directories { get; set; }
        public required IEnumerable<MediaInfo> MediaInfos { get; set; }
    }

    public class MediaInfo
    {
        public required string FileName { get; set; }
        public long FileSize { get; set; }
        public int? Duration { get; set; }
        public DateOnly? LastDate { get; set; }
        public float Progress { get; set; }
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
