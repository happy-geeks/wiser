using System.IO;
using System.Xml;
using System.Xml.Serialization;
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
            // TODO: Check if this is still needed.
            // For backwards compatibility, we need to remove queries elements from the xml.
            // This is because query elements are now allowed to exist within configuration instead of just configuration > queries.
            xml = xml.Replace("<Queries>", "");
            xml = xml.Replace("</Queries>", "");

            // Do the same for http APIs.
            xml = xml.Replace("<HttpApis>", "");
            xml = xml.Replace("</HttpApis>", "");

            var serializer = new XmlSerializer(typeof(TemplateWtsConfigurationModel));

            using var stringReader = new StringReader(xml);
            var configuration = (TemplateWtsConfigurationModel)serializer.Deserialize(stringReader);
            return configuration;
        }

        /// <inheritdoc />
        public string ParseObjectToXml(TemplateWtsConfigurationModel data)
        {
            var serializer = new XmlSerializer(typeof(TemplateWtsConfigurationModel));

            using var stringWriter = new StringWriter();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "\t"
            };

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", ""); // Remove the xmlns attribute

            using var writer = XmlWriter.Create(stringWriter, settings);
            serializer.Serialize(writer, data, namespaces);

            var xml = stringWriter.ToString();
            return xml;
        }
    }
}