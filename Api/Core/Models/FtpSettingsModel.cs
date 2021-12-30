using Api.Core.Enums;

namespace Api.Core.Models
{
    /// <summary>
    /// A model for settings for an FTP server.
    /// </summary>
    public class FtpSettingsModel
    {
        public string Host { get; set; }

        public FtpModes Mode { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string RootDirectory { get; set; }
    }
}
