using Api.Modules.Templates.Attributes;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    public abstract class ActionModel
    {
        public int TimeId { get; set; }
        
        public int Order { get; set; }
        
        [CanBeNull]
        public string ResultSetName { get; set; }
        
        [CanBeNull]
        public string UseResultSet { get; set; }
        
        [CanBeNull]
        public HashSettingsModel HashSettings { get; set; } = new()
        {
            Algorithm = HashAlgorithms.SHA256,
            Representation = HashRepresentations.Base64
        };
        
        [CanBeNull]
        public string OnlyWithStatusCode { get; set; }
        
        [CanBeNull]
        public string OnlyWithSuccessState { get; set; }
        
        [CanBeNull]
        public LogSettings LogSettings { get; set; }
    }
}