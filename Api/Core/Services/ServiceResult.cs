using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Api.Core.Services;

/// <summary>
/// A class to be used in services, to work with <see cref="HttpResponseMessage">HttpResponseMessages</see>.
/// </summary>
/// <typeparam name="T">The type that will be returned by the method in the service.</typeparam>
public class ServiceResult<T> : ActionResult
{
    /// <summary>
    /// The actual result of the method.
    /// </summary>
    public T ModelObject { get; set; }

    /// <summary>
    /// The <see cref="HttpStatusCode">StatusCode</see> to be returned to the client.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// The detailed error message to be returned tot he client. Leave empty if there is no error.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Initialize a new instance of the <see cref="ServiceResult{T}">ServiceResult</see>.
    /// </summary>
    public ServiceResult()
    {
        StatusCode = HttpStatusCode.OK;
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="ServiceResult{T}">ServiceResult</see> and set the <see cref="ModelObject">ModelObject</see>.
    /// </summary>
    /// <param name="modelObject"></param>
    public ServiceResult(T modelObject)
    {
        StatusCode = HttpStatusCode.OK;
        ModelObject = modelObject;
    }

    /// <summary>
    /// Generate a <see cref="HttpResponseMessage">HttpResponseMessage</see> that can be directly returned by the Controller.
    /// </summary>
    /// <returns>The <see cref="HttpResponseMessage">HttpResponseMessage</see> to be used by the controller.</returns>
    public ActionResult GetHttpResponseMessage(string contentType = null)
    {
        ActionResult response;

        if (!String.IsNullOrEmpty(ErrorMessage))
        {
            response = new ContentResult
            {
                Content = ErrorMessage,
                StatusCode = (int)StatusCode,
                ContentType = contentType ?? MediaTypeNames.Application.Json
            };
        }
        else if (ModelObject != null && StatusCode != HttpStatusCode.NoContent)
        {
            response = GetContentForModelObject(contentType);
        }
        else
        {
            response = new StatusCodeResult((int)StatusCode);
        }

        return response;
    }

    private ActionResult GetContentForModelObject(string contentType = null)
    {
        if (ModelObject is string && !String.IsNullOrWhiteSpace(contentType) && !String.Equals(contentType, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
        {
            return new ContentResult
            {
                ContentType = contentType,
                StatusCode = (int)StatusCode,
                Content = ModelObject?.ToString()
            };
        }

        return new ObjectResult(ModelObject) { StatusCode = (int)StatusCode };
    }
}