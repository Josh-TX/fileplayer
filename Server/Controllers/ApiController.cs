using Microsoft.AspNetCore.Mvc;
using System.IO;
using static System.Net.WebRequestMethods;

namespace FilePlayer.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private FileInfoService _fileInfoService;
        private SettingsService _settingsService;
        private DownloadService _downloadService;

        public ApiController(
            FileInfoService fileInfoService,
            SettingsService settingsService,
            DownloadService downloadService
            ) {
            _fileInfoService = fileInfoService;
            _settingsService = settingsService;
            _downloadService = downloadService;
        }

        private readonly string _dataFolderPath = Path.Combine(BasePathHelper.BasePath, "data");
        private IDictionary<(Guid, long), (DateOnly, double)> _progressData = new SortedList<(Guid, long), (DateOnly, double)>();

        [HttpGet]
        [Route("dir-contents")]
        public ActionResult<DirContentsResponse> GetDirContents(
            [FromQuery] string path = "",
            [FromQuery] string filter = "",
            [FromQuery] bool considerFolderContents = false,
            [FromQuery] bool regex = false,
            [FromQuery] bool typoTolerance = false,
            [FromQuery] bool anyOrder = false)
        {
            if (path == "" && !Directory.Exists(_dataFolderPath))
            {
                Directory.CreateDirectory(_dataFolderPath);
            } 
            string fullDirectory = Path.Combine(_dataFolderPath, path);
            if (!Directory.Exists(fullDirectory))
            {
                if (System.IO.File.Exists(fullDirectory)) {
                    return BadRequest("directory was a file");
                } else
                {
                    return NotFound($"Directory '{path}' not found.");
                }
            }
            var folderFileInfos = Directory.GetDirectories(fullDirectory).Select(d => new FileInfo(d)).ToList();
            var mediaFileInfos = Directory.GetFiles(fullDirectory).Select(f => new FileInfo(f)).ToList();
            if (!string.IsNullOrEmpty(filter))
            {
                var filterManager = new FilterManager(filter, considerFolderContents, regex, typoTolerance, anyOrder);
                filterManager.ApplyFilter(folderFileInfos);
                filterManager.ApplyFilter(mediaFileInfos);
            }
            var folderInfos = folderFileInfos.Select(fileInfo =>
            {
                var tuple = GetFolderSizeAndFileCount(fileInfo.FullName);
                return new FolderInfo
                {
                    FolderName = fileInfo.Name,
                    MediaFileCount = tuple.MediaFileCount,
                    MediaDiskSize = tuple.MediaDiskSize,
                    ModifyDate = fileInfo.LastWriteTime,
                };
            }).ToList();
            var mediaInfos = mediaFileInfos.Select(fileInfo =>
            {
                var size = fileInfo.Length;
                var fileId = FileIdHelper.GetId(fileInfo.Name, size);
                var info = _fileInfoService.GetInfoTuple(fileId);
                return new MediaInfo
                {
                    FileName = fileInfo.Name,
                    Duration = info?.Item2,
                    FileSize = size,
                    ModifyDate = fileInfo.LastWriteTime,
                    Progress = info?.Item1,
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
                return NotFound($"Directory '{path}' not found.");
            }
            var directories = Directory.GetDirectories(fullDirectory).Select(d => Path.GetFileName(d)).ToList();
            var fileNames = Directory.GetFiles(fullDirectory).Select(f => Path.GetFileName(f)).ToList();
            var mediaFilenames = fileNames.Where(filename => FileTypeHelper.IsMediaFile(filename));
            var taskRunner = new TaskPoolRunner(2);//run up to 2 tasks concurrently
            var durations = await taskRunner.Run(mediaFilenames, async fileName =>
            {
                var fullPath = Path.Combine(fullDirectory, fileName);
                var size = new FileInfo(fullPath).Length;
                var fileId = FileIdHelper.GetId(fileName, size);
                int? duration;
                try
                {
                    duration = await _fileInfoService.CalculateDuration(fileId, fullPath);
                } catch (Exception)
                {
                    //sometimes there's a failure when reading concurrently
                    duration = await _fileInfoService.CalculateDuration(fileId, fullPath);
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
            var fullPath = Path.Combine(_dataFolderPath, path);
            var fileInfo = new FileInfo(fullPath);
            var fileName = Path.GetFileName(path);
            if (!fileInfo.Exists)
            {
                return NotFound(new { Message = "file not found." });
            }
            var size = fileInfo.Length;
            var fileId = FileIdHelper.GetId(fileName, size);
            var duration = await _fileInfoService.CalculateDuration(fileId, fullPath);
            var infoTuple = _fileInfoService.GetInfoTuple(fileId);
            var mediaInfo = new MediaInfo
            {
                FileName = fileName,
                Duration = infoTuple?.Item2,
                FileSize = size,
                ModifyDate = fileInfo.LastWriteTime,
                Progress = infoTuple?.Item1,
            };
            return Ok(mediaInfo);
        }

        [HttpPost]
        [Route("update-progress")]
        public ActionResult UpdateProgress([FromQuery] double progress, [FromQuery] string path = "")
        {
            if (double.IsNaN(progress))
            {
                return BadRequest("progress is NaN");
            }
            if (!FileTypeHelper.IsMediaFile(path))
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
            _fileInfoService.SetProgress(fileId, Convert.ToSingle(progress));
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

        [HttpPost]
        [Route("upload-files")]
        public async Task<IActionResult> UploadFiles([FromForm] IFormFile file, [FromQuery] string path = "")
        {
            string fullDirectory = Path.Combine(_dataFolderPath, path);
            if (!Directory.Exists(fullDirectory))
            {
                return NotFound(new { Message = "Directory not found." });
            }
            if (file.Length > 0)
            {
                var filePath = Path.Combine(fullDirectory, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("delete-items")]
        public IActionResult DeleteItems([FromBody] DeleteItemsRequest request)
        {
            if (request.FilePaths.Any(z => z == ""))
            {
                return BadRequest("can't delete root directory");
            }
            foreach(var filepath in request.FilePaths)
            {
                string fullPath = Path.Combine(_dataFolderPath, filepath);
                if (System.IO.File.Exists(fullPath))
                {
                    var size = new FileInfo(fullPath).Length;
                    var fileId = FileIdHelper.GetId(Path.GetFileName(filepath), size);
                    System.IO.File.Delete(fullPath);
                    _fileInfoService.FileRemoved(fileId);
                }
                else if (Directory.Exists(fullPath))
                {
                    var fileIds = GetFileIdsRecursive(fullPath);
                    System.IO.Directory.Delete(fullPath, recursive: true);
                    foreach (var fileId in fileIds)
                    {
                        _fileInfoService.FileRemoved(fileId);
                    }
                }
                else
                {
                    //don't error since it's a bulk operation
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("rename")]
        public IActionResult Rename([FromQuery] string newName, [FromQuery] string path = "")
        {
            if (newName.Contains("/"))
            {
                return BadRequest("invalid name");
            }
            string fullPath = Path.Combine(_dataFolderPath, path);
            if (path == "")
            {
                return BadRequest("can't rename root directory");
            }
            if (System.IO.File.Exists(fullPath))
            {
                var parentPath = Path.GetDirectoryName(fullPath)!;
                var newFullPath = Path.Combine(parentPath, newName);
                if (System.IO.File.Exists(newFullPath) || Directory.Exists(newFullPath))
                {
                    return BadRequest("new name already taken");
                }
                var size = new FileInfo(fullPath).Length;
                System.IO.File.Move(fullPath, newFullPath);
                _fileInfoService.FileRenamed(Path.GetFileName(fullPath), Path.GetFileName(newFullPath), size);
            }
            else if (Directory.Exists(fullPath))
            {
                var parentPath = Path.GetDirectoryName(fullPath)!;
                var newFullPath = Path.Combine(parentPath, newName);
                if (System.IO.File.Exists(newFullPath) || Directory.Exists(newFullPath))
                {
                    return BadRequest("new name already taken");
                }
                System.IO.Directory.Move(fullPath, newFullPath);
            }
            else
            {
                return NotFound(new { Message = "File or Directory not found." });
            }
            return Ok();
        }

        [HttpPost]
        [Route("copy-items")]
        public IActionResult CopyItems([FromBody] CopyItemsRequest request)
        {
            if (request.FilePaths.Any(filepath => (request.DestinationDir + "/").StartsWith(filepath + "/")))
            {
                return BadRequest("DestinationDir is a subdirectory of a dir being moved");
            }

            //first validate the copy/move
            foreach (var file in request.FilePaths)
            {
                var oldPath = Path.Combine(_dataFolderPath, file);
                var newPath = Path.Combine(_dataFolderPath, request.DestinationDir, Path.GetFileName(file));
                if (System.IO.File.Exists(oldPath))
                {
                    if (System.IO.File.Exists(newPath))
                    {
                        return BadRequest($"Destination file {newPath} already exists");
                    }
                }
                else if (Directory.Exists(oldPath))
                {
                    var errorMessage = CanCopyOrMoveDirectory(oldPath, newPath);
                    if (errorMessage != null)
                    {
                        return BadRequest(errorMessage);
                    }
                }
                else
                {
                    return BadRequest($"No file or directory {oldPath}");
                }
            }

            //create DestinationDir if needed
            Directory.CreateDirectory(Path.Combine(_dataFolderPath, request.DestinationDir));

            //actually copy/move the files & directories
            foreach (var file in request.FilePaths)
            {
                var oldPath = Path.Combine(_dataFolderPath, file);
                var newPath = Path.Combine(_dataFolderPath, request.DestinationDir, Path.GetFileName(file));
                if (System.IO.File.Exists(oldPath))
                {
                    if (request.IsMove)
                    {
                        System.IO.File.Move(oldPath, newPath);
                    }
                    else
                    {
                        System.IO.File.Copy(oldPath, newPath);
                    }
                }
                else if (Directory.Exists(oldPath))
                {
                    CopyDirectory(oldPath, newPath, request.IsMove);
                    if (request.IsMove)
                    {
                        Directory.Delete(oldPath);
                    }
                }
            }
            return Ok();
        }

        /// <summary>
        /// Recursively checks if oldPath (a directory) can be moved OR merged with (newPath). returns null if directories can be moved, otherwise returns a message explaining why it can't be moved
        /// </summary>
        private string? CanCopyOrMoveDirectory(string oldPath, string newPath)
        {
            if (!Directory.Exists(newPath))
            {
                return null;//the directory doesn't exist yet, so we're good
            }
            var oldPathfiles = Directory.GetFiles(oldPath);
            foreach (var oldPathfile in oldPathfiles)
            {
                string newPathFile = Path.Combine(newPath, Path.GetFileName(oldPathfile));
                if (System.IO.File.Exists(newPathFile))
                {
                    return "Destination file " + newPathFile + " already exists";
                }
                if (Directory.Exists(newPathFile))
                {
                    return "Destination directory " + newPathFile + " already exists";
                }
            }
            var oldPathDirectories = Directory.GetDirectories(oldPath);
            foreach (var oldPathDirectory in oldPathDirectories)
            {
                string newPathDirectory = Path.Combine(newPath, Path.GetFileName(oldPathDirectory));
                if (System.IO.File.Exists(newPathDirectory))
                {
                    return "Destination file " + newPathDirectory + " already exists";
                }
                if (Directory.Exists(newPathDirectory))
                {
                    var result = CanCopyOrMoveDirectory(oldPathDirectory, newPathDirectory);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private void CopyDirectory(string oldPath, string newPath, bool isMove)
        {
            Directory.CreateDirectory(newPath);
            var oldPathfiles = Directory.GetFiles(oldPath);
            foreach (var oldPathfile in oldPathfiles)
            {
                string newPathFile = Path.Combine(newPath, Path.GetFileName(oldPathfile));
                if (isMove)
                {
                    System.IO.File.Move(oldPathfile, newPathFile);
                }
                else
                {
                    System.IO.File.Copy(oldPathfile, newPathFile);
                }
            }
            var oldPathDirectories = Directory.GetDirectories(oldPath);
            foreach (var oldPathDirectory in oldPathDirectories)
            {
                string newPathDirectory = Path.Combine(newPath, Path.GetFileName(oldPathDirectory));
                CopyDirectory(oldPathDirectory, newPathDirectory, isMove);
                if (isMove)
                {
                    Directory.Delete(oldPathDirectory);
                }
            }
        }

        [HttpPost]
        [Route("create-dir")]
        public IActionResult CreateDir([FromQuery] string path = "")
        {
            if (path == "")
            {
                return BadRequest("can't create root directory");
            }
            string fullPath = Path.Combine(_dataFolderPath, path);
            if (Directory.Exists(fullPath) || System.IO.File.Exists(fullPath))
            {
                return BadRequest("name already taken");
            }
            Directory.CreateDirectory(fullPath);
            return Ok();
        }

        [HttpGet]
        [Route("get-url-info")]
        public async Task<IActionResult> GetUrlInfo([FromQuery] string url)
        {
            var info = await _downloadService.GetInfo(url);
            return Ok(info);
        }

        [HttpPost]
        [Route("upload-from-url")]
        public IActionResult UploadFromUrl([FromBody] UploadFromUrlRequest request)
        {
            string fullDirectory = Path.Combine(_dataFolderPath, request.Path);
            if (!Directory.Exists(fullDirectory))
            {
                return NotFound(new { Message = "Directory not found." });
            }
            _downloadService.StartDownload(fullDirectory, request.Url, request.PreferredHeight, request.OverrideName, request.UseMDate, request.CompatCodec);
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
                totalSize += file.Length;
                mediaDiskSize++;
            }
            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                var subDirResult = GetFolderSizeAndFileCount(subDirInfo.FullName);
                totalSize += subDirResult.MediaDiskSize;
                mediaDiskSize += subDirResult.MediaFileCount;
            }
            return (totalSize, mediaDiskSize);
        }

        private List<Guid> GetFileIdsRecursive(string folderPath)
        {
            var fileIds = new List<Guid>();
            var dirInfo = new DirectoryInfo(folderPath);
            if (!dirInfo.Exists)
            {
                throw new DirectoryNotFoundException($"The directory {folderPath} does not exist.");
            }
            foreach (var file in dirInfo.GetFiles())
            {
                fileIds.Add(FileIdHelper.GetId(file.Name, file.Length));
            }
            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                var subDirFileIds = GetFileIdsRecursive(subDirInfo.FullName);
                fileIds.AddRange(subDirFileIds);
            }
            return fileIds;
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
        public float? Progress { get; set; }
    }

    public class UrlInfo
    {
        public required string Title { get; set; }
        public int? Duration { get; set; }
        public string? ThumbnailUrl { get; set; }
        public DateTime? PublishDate { get; set; }
        public required IEnumerable<string> Heights { get; set; }
        public bool AudioOnly { get; set; }

    }

    public class UploadFromUrlRequest
    {
        public required string Path { get; set; }
        public required string Url { get; set; }
        public int? PreferredHeight { get; set; }
        public string? OverrideName { get; set; }
        public bool UseMDate { get; set; }
        public bool CompatCodec { get; set; }
    }

    public class CopyItemsRequest
    {
        public required IEnumerable<string> FilePaths { get; set; }
        public required string DestinationDir { get; set; }
        public required bool IsMove { get; set; }
    }

    public class DeleteItemsRequest
    {
        public required IEnumerable<string> FilePaths { get; set; }
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
