using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Core.Helpers
{
    /// <summary>
    /// Helpers for embedded resources.
    /// </summary>
    public class ResourceHelpers
    {
        /// <summary>
        /// Get the contents from an embedded resource.
        /// Note that the build action for these files have to be set to "Embedded resource" before you can use this function.
        /// </summary>
        /// <param name="name">The fully qualified name to the file (e.g. Api.Modules.Babel.Scripts.Polyfills.babel.js).</param>
        /// <returns>The contents of the embedded resource.</returns>
        public static async Task<string> ReadTextResourceFromAssemblyAsync(string name)
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream == null)
            {
                return "";
            }

            using var streamReader = new StreamReader(stream);
            return await streamReader.ReadToEndAsync();
        }
    }
}
