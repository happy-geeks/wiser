using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;
using GeeksCoreLibrary.Modules.Templates.Enums;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for all settings of a template.
    /// </summary>
    public class TemplateSettingsModel
    {
        public int TemplateId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string EditorValue { get; set; }
        [JsonIgnore]
        public string MinifiedValue { get; set; }
        public int Version { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }
        public TemplateTypes Type { get; set; }
        public int Ordering { get; set; }
        public LinkedTemplatesModel LinkedTemplates { get; set; }
        public PublishedEnvironmentModel PublishedEnvironments { get; set; }

        // HTML settings
        public int UseCache { get; set; }
        public int CacheMinutes { get; set; }
        public bool HandleRequests { get; set; }
        public bool HandleSession { get; set; }
        public bool HandleObjects { get; set; }
        public bool HandleStandards { get; set; }
        public bool HandleTranslations { get; set; }
        public bool HandleDynamicContent { get; set; }
        public bool HandleLogicBlocks { get; set; }
        public bool HandleMutators { get; set; }
        public bool LoginRequired { get; set; }
        public string LoginUserType { get; set; }
        public string LoginSessionPrefix { get; set; }
        public string LoginRole { get; set; }
        public string PreLoadQuery { get; set; }

        // Css/Scss/Js settings.
        public ResourceInsertModes InsertMode { get; set; }
        public bool LoadAlways { get; set; }
        public string UrlRegex { get; set; }
        public List<string> ExternalFiles { get; set; } = new();
        public bool IsScssIncludeTemplate { get; set; }
        public bool UseInWiserHtmlEditors { get; set; }

        // Js only settings.
        public bool DisableMinifier { get; set; }
        
        // Query settings,
        public bool GroupingCreateObjectInsteadOfArray { get; set; }
        public string GroupingPrefix { get; set; }
        public string GroupingKey { get; set; }
        public string GroupingKeyColumnName { get; set; }
        public string GroupingValueColumnName { get; set; }

        public Dictionary<string, object> Changes { get; set; }
    }
}
