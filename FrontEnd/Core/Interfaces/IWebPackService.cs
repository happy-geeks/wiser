using System.Threading.Tasks;

namespace FrontEnd.Core.Interfaces;

public interface IWebPackService
{
    /// <summary>
    /// Load the webpack manifest file into memory.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Gets the full filename and location for a file from the manifest.
    /// </summary>
    /// <param name="fileName">The name of the file to get the location for.</param>
    /// <returns>The full location to the file, including path and file name.</returns>
    Task<string> GetManifestFileAsync(string fileName);
}