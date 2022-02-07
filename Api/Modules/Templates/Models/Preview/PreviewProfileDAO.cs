using System;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewProfileDao
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string RawVariables { get; set; }

        public PreviewProfileDao (int id, string name, string url, string variables)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
            this.RawVariables = variables;
        }
    }
}
