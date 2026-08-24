using System.Security.Cryptography;
using System.Text;

namespace CoreLib.Common;

public static class EncryptionHelper
{
    private static readonly string key = "your-256-bit-key";
    private static readonly string iv = "your-128-bit-iv";

    public static string EncryptString(string plainText)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ivBytes = Encoding.UTF8.GetBytes(iv);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }

    public static string DecryptString(string cipherText)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ivBytes = Encoding.UTF8.GetBytes(iv);
        var cipherBytes = Convert.FromBase64String(cipherText);

        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream(cipherBytes))
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}