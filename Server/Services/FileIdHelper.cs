using Murmur;
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
}
