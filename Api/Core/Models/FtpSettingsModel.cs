using Api.Core.Enums;

namespace Api.Core.Models;

/// <summary>
/// A model for settings for an FTP server.
/// </summary>
public class FtpSettingsModel
{
    /// <summary>
    /// The Host of the FTP server
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// The Mode of the FTP server
    /// </summary>
    public FtpModes Mode { get; set; }

    /// <summary>
    /// The username needed for the FTP server
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// The password needed for the FTP server
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// The root directory for the FTP server
    /// </summary>
    public string RootDirectory { get; set; }
}