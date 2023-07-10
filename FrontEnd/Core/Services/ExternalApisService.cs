using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace FrontEnd.Core.Services;

/// <inheritdoc cref="IExternalApisService" />
public class ExternalApisService : IExternalApisService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Creates a new instance of <see cref="ExternalApisService"/>.
    /// </summary>
    public ExternalApisService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<ContentResult> ProxyAsync()
    {
        if (httpContextAccessor.HttpContext == null)
        {
            throw new Exception("No http context found.");
        }

        var apiUrlValue = httpContextAccessor.HttpContext.Request.Headers["X-Api-Url"].ToString();
        if (String.IsNullOrWhiteSpace(apiUrlValue))
        {
            return new ContentResult
            {
                StatusCode = (int) HttpStatusCode.BadRequest,
                Content = "Header 'X-Api-Url' is not set."
            };
        }

        // Start building URL for the external API.
        var apiUrl = new UriBuilder(apiUrlValue);
        var apiQueryString = HttpUtility.ParseQueryString(apiUrl.Query);
        foreach (var queryStringValue in httpContextAccessor.HttpContext.Request.Query)
        {
            apiQueryString[queryStringValue.Key] = queryStringValue.Value;
        }

        apiUrl.Query = apiQueryString.ToString()!;
        
        // Get the HTTP method that was used.
        Method apiMethod;
        var xHttpMethod = httpContextAccessor.HttpContext.Request.Headers["X-Http-Method"].ToString();
        if (!String.IsNullOrWhiteSpace(xHttpMethod))
        {
            if (!Enum.TryParse(xHttpMethod, true, out apiMethod))
            {
                return new ContentResult
                {
                    StatusCode = (int) HttpStatusCode.BadRequest,
                    Content = "Invalid 'X-Http-Method' value."
                };
            }
        }
        else
        {
            if (!Enum.TryParse(httpContextAccessor.HttpContext.Request.Method, true, out apiMethod))
            {
                return new ContentResult
                {
                    StatusCode = (int) HttpStatusCode.BadRequest,
                    Content = "Unknown HTTP method used."
                };
            }
        }

        var restClient = new RestClient(apiUrlValue);
        var restRequest = new RestRequest("", apiMethod);

        // Copy all headers to the request to the external API.
        const string headerPrefix = "X-Extra-";
        foreach (var header in httpContextAccessor.HttpContext.Request.Headers)
        {
            if (!header.Key.ToLower().StartsWith(headerPrefix.ToLower()))
            {
                continue;
            }

            restRequest.AddHeader(header.Key[headerPrefix.Length..], header.Value);
        }

        // Copy the request body to the request to the external API.
        using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
        var bodyString = await reader.ReadToEndAsync();
        restRequest.AddParameter(httpContextAccessor.HttpContext.Request.ContentType, bodyString, ParameterType.RequestBody);

        // Do the request and get the results.
        var apiResult = await restClient.ExecuteAsync(restRequest);
        
        // Return the result of the external API.
        var result = new ContentResult
        {
            StatusCode = (int) apiResult.StatusCode,
            Content = apiResult.Content,
            ContentType = apiResult.ContentType
        };

        return result;
    }
}