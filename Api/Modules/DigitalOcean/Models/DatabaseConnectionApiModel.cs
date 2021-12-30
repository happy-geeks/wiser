namespace Api.Modules.DigitalOcean.Models
{
    public class DatabaseConnectionApiModel
    {
        public string Protocol { get; set; }
        public string Uri { get; set; }
        public string Database { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Ssl { get; set; }
    }
}
