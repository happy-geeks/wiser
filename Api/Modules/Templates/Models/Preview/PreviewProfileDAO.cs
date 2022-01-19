using System;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewProfileDAO
    {
        Int64 id;
        string name;
        string url;
        string rawVariables;

        public PreviewProfileDAO (Int64 id, string name, string url, string variables)
        {
            this.id = id;
            this.name = name;
            this.url = url;
            this.rawVariables = variables;
        }

        public Int64 GetId()
        {
            return this.id;
        }

        public string GetName()
        {
            return this.name;
        }

        public string GetUrl()
        {
            return this.url;
        }

        public string GetRawVariables()
        {
            return this.rawVariables;
        }
    }
}
