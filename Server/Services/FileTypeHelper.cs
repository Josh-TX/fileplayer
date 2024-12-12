using Murmur;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

public static class FileTypeHelper
{
    private static string[] _mediaExtensions = new[]{
        "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "3gp", "m4v", "ogv", "mpeg", "mpg", "f4v", "rmvb", "asf", "vob", "mxf", "divx",
        "mp3", "wav", "aac", "flac", "ogg", "alac", "m4a", "opus", "mid", "midi"
    };
    public static bool IsMediaFile(string filename)
    {
        return _mediaExtensions.Any(ext => filename.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase));
    }
}
