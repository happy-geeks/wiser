using System;
using System.Xml;
using Api.Modules.Templates.Models.Template;
using DocumentFormat.OpenXml.Drawing;

namespace FrontEnd.Modules.Templates.Models
{
    public class WtsConfigurationTabViewModel
    {
        public TemplateSettingsModel TemplateSettings { get; set; }

        public string getServiceName()
        {
            // Create an XmlDocument instance and load the XML string
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TemplateSettings.EditorValue);
            
            // Select the ServiceName element
            XmlNode serviceNameNode = xmlDoc.SelectSingleNode("/Configuration/ServiceName");
            
            if (serviceNameNode == null)
            {
                // Return an empty string since this field could be empty
                // (Shouldn't be empty in the end but it might be when creating a new config)
                Console.WriteLine("ServiceName node not found in XML file");
                return "";
            }
            return serviceNameNode.InnerText;
        }
        
        public string getConnectionString()
        {
            // Create an XmlDocument instance and load the XML string
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TemplateSettings.EditorValue);
            
            // Select the ServiceName element
            XmlNode connectionStringNode = xmlDoc.SelectSingleNode("/Configuration/ConnectionString");
            
            if (connectionStringNode == null)
            {
                // Return an empty string since this field could be empty
                // (Shouldn't be empty in the end but it might be when creating a new config)
                Console.WriteLine("ConnectionString node not found in XML file");
                return "";
            }
            return connectionStringNode.InnerText;
        }
    }
}