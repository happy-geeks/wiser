namespace Api.Modules.Templates.Models.Measurements;

/// <summary>
/// A model with settings of how/where to measure render times of a template or component.
/// </summary>
public class MeasurementSettings
{
    /// <summary>
    /// Gets or sets the ID of the template or component.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the development environment.
    /// </summary>
    public bool MeasureRenderTimesOnDevelopmentForCurrent { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of all templates or components on the development environment.
    /// </summary>
    public bool MeasureRenderTimesOnDevelopmentForEverything { get; set; }

    /// <summary>
    /// Gets or sets whether to measure all render times of this template/component or for all templates/components on the development environment.
    /// </summary>
    public bool MeasureRenderTimesOnDevelopment => MeasureRenderTimesOnDevelopmentForCurrent || MeasureRenderTimesOnDevelopmentForEverything;
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the test environment.
    /// </summary>
    public bool MeasureRenderTimesOnTestForCurrent { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of all templates or components on the test environment.
    /// </summary>
    public bool MeasureRenderTimesOnTestForEverything { get; set; }

    /// <summary>
    /// Gets or sets whether to measure all render times of this template/component or for all templates/components on the test environment.
    /// </summary>
    public bool MeasureRenderTimesOnTest => MeasureRenderTimesOnTestForCurrent || MeasureRenderTimesOnTestForEverything;
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the acceptance environment.
    /// </summary>
    public bool MeasureRenderTimesOnAcceptanceForCurrent { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of all templates or components on the acceptance environment.
    /// </summary>
    public bool MeasureRenderTimesOnAcceptanceForEverything { get; set; }

    /// <summary>
    /// Gets or sets whether to measure all render times of this template/component or for all templates/components on the acceptance environment.
    /// </summary>
    public bool MeasureRenderTimesOnAcceptance => MeasureRenderTimesOnAcceptanceForCurrent || MeasureRenderTimesOnAcceptanceForEverything;
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the live environment.
    /// </summary>
    public bool MeasureRenderTimesOnLiveForCurrent { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of all templates or components on the live environment.
    /// </summary>
    public bool MeasureRenderTimesOnLiveForEverything { get; set; }

    /// <summary>
    /// Gets or sets whether to measure all render times of this template/component or for all templates/components on the live environment.
    /// </summary>
    public bool MeasureRenderTimesOnLive => MeasureRenderTimesOnLiveForCurrent || MeasureRenderTimesOnLiveForEverything;
}