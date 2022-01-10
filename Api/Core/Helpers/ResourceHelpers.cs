using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Core.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceHelpers
    {
        /// <summary>
        /// Get a query from an SQL file (embedded resource).
        /// </summary>
        /// <param name="name">The name of the SQL file (without extension).</param>
        /// <returns>The contents of the SQL file.</returns>
        public static async Task<string> ReadTextResourceFromAssemblyAsync(string name)
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Api.Core.Queries.WiserInstallation.{name}.sql");
            if (stream == null)
            {
                return "";
            }

            using var streamReader = new StreamReader(stream);
            return await streamReader.ReadToEndAsync();
        }
    }
}
