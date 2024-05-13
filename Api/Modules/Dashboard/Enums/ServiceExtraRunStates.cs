namespace Api.Modules.Dashboard.Enums;

/// <summary>
/// All of the states a Service can be in when for an Extra Run
/// </summary>
public enum ServiceExtraRunStates
{
    /// <summary>
    /// Service is marked to make an extra run.
    /// </summary>
    Marked,
    
    /// <summary>
    /// Service is unmarked to make an extra run, extra run request has been cancelled.
    /// </summary>
    Unmarked,
    
    /// <summary>
    /// Service is currently running and state can't be changed.
    /// </summary>
    ServiceRunning,
    
    /// <summary>
    /// Service is not being run by an WTS and can't be started.
    /// </summary>
    WtsOffline
}