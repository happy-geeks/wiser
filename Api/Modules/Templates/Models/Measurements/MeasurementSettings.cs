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
    public bool MeasureRenderTimesOnDevelopment { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the test environment.
    /// </summary>
    public bool MeasureRenderTimesOnTest { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the acceptance environment.
    /// </summary>
    public bool MeasureRenderTimesOnAcceptance { get; set; }
    
    /// <summary>
    /// Gets or sets whether to measure all render times of this template or component on the live environment.
    /// </summary>
    public bool MeasureRenderTimesOnLive { get; set; }
}