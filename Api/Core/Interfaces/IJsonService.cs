using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Api.Core.Interfaces;

/// <summary>
/// A services for doing things with JSON string / objects.
/// </summary>
public interface IJsonService
{
    /// <summary>
    /// Encrypt values of certain properties in a JSON object.
    /// This will go recursively through the entire object to encrypt all properties with names that are set in the web.config.
    /// </summary>
    /// <param name="jsonObject">The JSON object.</param>
    /// <param name="encryptionKey">The encryption key to use.</param>
    /// <param name="extraPropertiesToEncrypt">Optional: If you need to encrypt any extra properties, that are not in the web.config, you can enter them here.</param>
    void EncryptValuesInJson(JToken jsonObject, string encryptionKey, List<string> extraPropertiesToEncrypt = null);
}