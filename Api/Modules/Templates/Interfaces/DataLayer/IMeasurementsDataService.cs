using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Measurements;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Interfaces.DataLayer;

/// <summary>
/// Data service for doing things in the database for measurements for templates and components.
/// </summary>
public interface IMeasurementsDataService
{
    /// <summary>
    /// Get rendering logs from database. You need to enter either a template ID or a component ID (not both), all other parameters are optional.
    /// </summary>
    /// <param name="templateId">The ID of the template to get the render logs for. Leave empty if you want to get everything or if you want to get logs from a component instead.</param>
    /// <param name="componentId">The ID of the component to get the render logs for. Leave empty if you want to get everything or if you want to get logs from a template instead.</param>
    /// <param name="version">The version of the template or component.</param>
    /// <param name="urlRegex">A regex for filtering logs on certain URLs/pages.</param>
    /// <param name="environment">The environment to get the logs for. Set to null to get the logs for all environments. Default value is null.</param>
    /// <param name="userId">The ID of the website user, if you want to get the logs for a specific user only.</param>
    /// <param name="languageCode">The language code that is used on the website, if you want to get the logs for a specific language only.</param>
    /// <param name="pageSize">The amount of logs to get. Set to 0 to get all of then. Default value is 500.</param>
    /// <param name="pageNumber">The page number. Default value is 1. Only applicable if pageSize is greater than zero.</param>
    /// <param name="getDailyAverage">Set this to true to get the average render time per day, instead of getting every single render log separately. Default is false.</param>
    /// <param name="start">Only get results from this start date and later.</param>
    /// <param name="end">Only get results from this end date and earlier.</param>
    /// <returns>A list of <see cref="RenderLogModel"/> with the results.</returns>
    Task<List<RenderLogModel>> GetRenderLogsAsync(int templateId = 0, int componentId = 0, int version = 0,
        string urlRegex = null, Environments? environment = null, ulong userId = 0,
        string languageCode = null, int pageSize = 500, int pageNumber = 1, 
        bool getDailyAverage = false, DateTime? start = null, DateTime? end = null);
}