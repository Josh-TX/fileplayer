using Murmur;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

public static class FileIdHelper
{
    private static HashAlgorithm _murmur128 = MurmurHash.Create128(managed: false);
    public static Guid GetId(string fileName, long size)
    {
        var bytes = _murmur128.ComputeHash(Encoding.UTF8.GetBytes(fileName));
        uint lastFourBytes = (uint)(size & 0xFFFFFFFF);
        byte[] lastFourByteArray = BitConverter.GetBytes(lastFourBytes);
        Array.Copy(lastFourByteArray, 0, bytes, 12, 4);
        return new Guid(bytes);
    }

    public static string IdToBase64(Guid fileId)
    {
        byte[] bytes = fileId.ToByteArray();
        string base64 = Convert.ToBase64String(bytes);
        return base64.TrimEnd('=');
    }

    public static Guid Base64ToId(string base64)
    {
        int padding = 4 - (base64.Length % 4);
        if (padding < 4)
        {
            base64 = base64.PadRight(base64.Length + padding, '=');
        }
        byte[] bytes = Convert.FromBase64String(base64);
        return new Guid(bytes);
    }
}
