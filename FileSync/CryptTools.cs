using System.Security.Cryptography;
using System.Text;

namespace FileSync
{
    public class CryptTools
    {
        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = MD5.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static byte[] GetHash(byte[] input)
        {
            using (HashAlgorithm algorithm = MD5.Create())
                return algorithm.ComputeHash(input);
        }

        public static string GetHashString(byte[] input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(input))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
