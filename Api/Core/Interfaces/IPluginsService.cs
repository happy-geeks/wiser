namespace Api.Core.Interfaces;

/// <summary>
/// A service for loading plugins and maybe other things in the future.
/// </summary>
public interface IPluginsService
{
    /// <summary>
    /// Load plugins for Wiser. This will load all DLL files in the given directory.
    /// This method should be called during the startup of the application, before registering the services.
    /// </summary>
    /// <remarks>
    /// This function does not do any checks on the DLL files. It will try to load all DLL files in the given directory.
    /// This is meant for plugins that are developed by Wiser developers, so we know they are safe to load.
    /// If we want to be able to load external plugins in the future,
    /// we will need to extend this function with some security checks and load the plugins in isolated contexts.
    /// </remarks>
    /// <param name="pluginsDirectory">The directory that contains the plugins.</param>
    void LoadPlugins(string pluginsDirectory);
}