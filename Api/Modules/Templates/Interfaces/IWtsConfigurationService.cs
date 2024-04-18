using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Templates.Models.Template.WtsModels;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// A service for doing things with wts configurations from the templates module in Wiser.
    /// </summary>
    public interface IWtsConfigurationService
    {
        /// <summary>
        /// Parse xml to an object.
        /// </summary>
        /// <param name="xml">The incoming xml string.</param>
        /// <returns>The parsed object</returns>
        TemplateWtsConfigurationModel ParseXmlToObject(string xml);
        
        /// <summary>
        /// Parse an object to xml.
        /// </summary>
        /// <param name="data">The incoming object with data.</param>
        /// <returns>The parsed xml</returns>
        string ParseObjectToXml(TemplateWtsConfigurationModel data);
    }
}