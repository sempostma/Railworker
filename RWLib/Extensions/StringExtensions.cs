using System.Security.Cryptography;
using System.Text;

public static class StringExtensions
{
    public static long GetConsistentHash(this string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            
            return BitConverter.ToInt64(hashBytes, 0);
        }
    }
}
