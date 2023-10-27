using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.Template.WtsModels;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="IWtsConfigurationService" />
    public class WtsConfigurationService : IWtsConfigurationService, IScopedService
    {
        /// <inheritdoc />
        public TemplateWtsConfigurationModel ParseXmlToObject(string xml)
        {
            // For backwards compatibility, we need to remove queries elements from the xml
            // This is because query elements are now allowed to exist within configuration instead of just configuration > queries
            xml = xml.Replace("<Queries>", "");
            xml = xml.Replace("</Queries>", "");
            
            // Do the same for http api's
            xml = xml.Replace("<HttpApis>", "");
            xml = xml.Replace("</HttpApis>", "");
            
            var serializer = new XmlSerializer(typeof(TemplateWtsConfigurationModel));

            using (var stringReader = new StringReader(xml))
            {
                var configuration = (TemplateWtsConfigurationModel)serializer.Deserialize(stringReader);

                return configuration;
            }
        }

        /// <inheritdoc />
        public string ParseObjectToXml(TemplateWtsConfigurationModel data)
        {
            var serializer = new XmlSerializer(typeof(TemplateWtsConfigurationModel));

            using (StringWriter stringWriter = new StringWriter())
            {
                var settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true; // Remove the XML declaration
                settings.Indent = true; // Indent the XML output
                settings.IndentChars = "    "; // Set the indent size to 4 spaces.

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", ""); // Remove the xmlns attribute

                var writer = XmlWriter.Create(stringWriter, settings);

                serializer.Serialize(writer, data, namespaces);

                var xml = stringWriter.ToString();

                return xml;
            }
        }
        
        /// <inheritdoc />
        public (string[], string[]) GetInputValues()
        {
            // Grab the input values from local enums
            var logMinimumLevels = Enum.GetNames(typeof(LogMinimumLevels));
            var runSchemeTypes = Enum.GetNames(typeof(RunSchemeTypes));
            
            return (logMinimumLevels, runSchemeTypes);
        }
    }
}