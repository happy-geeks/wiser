using System;
using System.Xml;
using FrontEnd.Modules.Templates.Models;

namespace FrontEnd.Modules.Templates.Services;

public class XmlParsingService
{
    public WtsConfigurationTabViewModel formatXmlToModel(string xmlData)
    {
        // Create an XmlDocument instance and load the XML string
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlData);
        
        // Get all the nodes in the XML file
        XmlNode serviceNameNode = xmlDoc.SelectSingleNode("/Configuration/ServiceName"); // Mandatory
        XmlNode connectionStringNode = xmlDoc.SelectSingleNode("/Configuration/ConnectionString"); // Mandatory
        
        // Create a new WtsConfigurationTabViewModel instance
        WtsConfigurationTabViewModel model = new WtsConfigurationTabViewModel
        {
            ServiceName = serviceNameNode.InnerText,
            ConnectionString = connectionStringNode.InnerText
        };
        
        // Return the model
        return model;
    }
    // public string getServiceName(string serviceName)
    // {
    //     // Create an XmlDocument instance and load the XML string
    //     XmlDocument xmlDoc = new XmlDocument();
    //     xmlDoc.LoadXml(serviceName);
    //         
    //     // Select the ServiceName element
    //     XmlNode serviceNameNode = xmlDoc.SelectSingleNode("/Configuration/ServiceName");
    //         
    //     if (serviceNameNode == null)
    //     {
    //         // Return an empty string since this field could be empty
    //         // (Shouldn't be empty in the end but it might be when creating a new config)
    //         Console.WriteLine("ServiceName node not found in XML file");
    //         return "";
    //     }
    //     return serviceNameNode.InnerText;
    // }
    //     
    // public string getConnectionString(string connectionString)
    // {
    //     // Create an XmlDocument instance and load the XML string
    //     XmlDocument xmlDoc = new XmlDocument();
    //     xmlDoc.LoadXml(connectionString);
    //         
    //     // Select the ServiceName element
    //     XmlNode connectionStringNode = xmlDoc.SelectSingleNode("/Configuration/ConnectionString");
    //         
    //     if (connectionStringNode == null)
    //     {
    //         // Return an empty string since this field could be empty
    //         // (Shouldn't be empty in the end but it might be when creating a new config)
    //         Console.WriteLine("ConnectionString node not found in XML file");
    //         return "";
    //     }
    //     return connectionStringNode.InnerText;
    // }
}