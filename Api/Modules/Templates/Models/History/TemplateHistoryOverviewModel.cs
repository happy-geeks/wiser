using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.History;

/// <summary>
/// A model class to create an overview for the history of templates
/// </summary>
public class TemplateHistoryOverviewModel
{
    /// <summary>
    /// The ID of the template of which the history must be gotten from.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int TemplateId { get; set; }
        
    /// <summary>
    /// The version numbers of the different environments the template can be published to.
    /// </summary>
    public PublishedEnvironmentModel PublishedEnvironment { get; set; }
        
    /// <summary>
    /// The history of the template, this contains what has changed but also when and by who.
    /// </summary>
    public List<TemplateHistoryModel> TemplateHistory { get; set; }
        
    /// <summary>
    /// The history of the publication of the template
    /// </summary>
    public List<PublishHistoryModel> PublishHistory { get; set; }
}