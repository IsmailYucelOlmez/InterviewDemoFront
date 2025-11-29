using System.Security.Cryptography;
using System.Text;

namespace CommunicationApp.Helpers;

public static class HashHelper
{
    public static string ComputeMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public static string ComputeSHA1Hash(string input)
    {
        using var sha1 = SHA1.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha1.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}

