using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Api.Core.Interfaces;
using Api.Core.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Core.Services;

/// <inheritdoc cref="IJsonService" />
public class JsonService : IJsonService, ITransientService
{
    private readonly ApiSettings apiSettings;

    /// <summary>
    /// Creates a new instance of JsonService.
    /// </summary>
    public JsonService(IOptions<ApiSettings> apiSettings)
    {
        this.apiSettings = apiSettings.Value;
    }

    /// <inheritdoc />
    public void EncryptValuesInJson(JToken jsonObject, string encryptionKey, List<string> extraPropertiesToEncrypt = null)
    {
        if (apiSettings.JsonPropertiesToAlwaysEncrypt == null || !apiSettings.JsonPropertiesToAlwaysEncrypt.Any())
        {
            // No point in executing this function if there are no properties to encrypt.
            return;
        }

        if (jsonObject == null)
        {
            return;
        }

        foreach (var child in jsonObject.Children())
        {
            switch (child)
            {
                case JArray childAsArray:
                {
                    foreach (var item in childAsArray)
                    {
                        EncryptValuesInJson(item, encryptionKey, extraPropertiesToEncrypt);
                    }

                    break;
                }
                case JObject childAsObject:
                {
                    EncryptValuesInJson(childAsObject, encryptionKey, extraPropertiesToEncrypt);
                    break;
                }
                case JProperty childAsProperty:
                {
                    switch (childAsProperty.Value)
                    {
                        case JObject valueAsObject:
                        {
                            EncryptValuesInJson(valueAsObject, encryptionKey, extraPropertiesToEncrypt);
                            break;
                        }
                        case JArray valueAsArray:
                        {
                            foreach (var item in valueAsArray)
                            {
                                EncryptValuesInJson(item, encryptionKey, extraPropertiesToEncrypt);
                            }

                            break;
                        }
                        case JValue value:
                        {
                            var name = childAsProperty.Name;
                            if (apiSettings.JsonPropertiesToAlwaysEncrypt.Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase)) || (extraPropertiesToEncrypt != null && extraPropertiesToEncrypt.Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase))))
                            {
                                value.Value = value.ToString(CultureInfo.InvariantCulture).EncryptWithAesWithSalt(encryptionKey, true);
                            }

                            break;
                        }
                        default:
                            throw new Exception($"Unsupported JSON value type in 'EncryptValuesInJson': {childAsProperty.Value.GetType().Name}");
                    }


                    break;
                }
                default:
                    throw new Exception($"Unsupported JSON type in 'EncryptValuesInJson': {child.GetType().Name}");
            }
        }
    }
}