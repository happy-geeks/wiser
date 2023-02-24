namespace Api.Modules.Dashboard.Enums;

/// <summary>
/// An enum with all the states that a ServicePause can have
/// </summary>
public enum ServicePauseStates
{
    /// <summary>
    /// The Service is Paused
    /// </summary>
    Paused,
    
    /// <summary>
    /// The Service is Unpaused
    /// </summary>
    Unpaused,
    
    /// <summary>
    /// The Service will Pause after a run has finished
    /// </summary>
    WillPauseAfterRunFinished
}