using System.Security.Cryptography;
using System.Text;

namespace Api.Core.Helpers
{
    /// <summary>
    /// Class that contains String helpers
    /// </summary>
    public class StringHelpers
    {
        /// <summary>
        /// Create a SHA512 hash with a salt.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <param name="salt">The salt to use in the hash.</param>
        /// <returns>The SHA256 hash of input+salt.</returns>
        public static string CreateSha512Hash(string input, string salt)
        {
            using (var hasher = SHA512.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                var data = hasher.ComputeHash(Encoding.UTF8.GetBytes(input + salt));

                // Create a new Stringbuilder to collect the bytes and create a string.
                var stringBuilder = new StringBuilder();

                // Loop through each byte of the hashed data and format each one as a hexadecimal string.
                foreach (var t in data)
                {
                    stringBuilder.Append(t.ToString("x2"));
                }

                // Return the hexadecimal string.
                return stringBuilder.ToString();
            }
        }
    }
}
