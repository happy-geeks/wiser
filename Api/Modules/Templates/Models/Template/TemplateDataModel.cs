using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateDataModel
    {
        public int templateid;
        public string name;
        public string editorValue;
        public int version;
        public DateTime changed_on;
        public string changed_by;

        //Advanced settings
        public int useCache;
        public int cacheMinutes;
        public Boolean handleRequests;
        public Boolean handleSession;
        public Boolean handleObjects;
        public Boolean handleStandards;
        public Boolean handleTranslations;
        public Boolean handleDynamicContent;
        public Boolean handleLogicBlocks;
        public Boolean handleMutators;
        public Boolean loginRequired;
        public string loginUserType;
        public string loginSessionPrefix;
        public string loginRole;

        public Dictionary<string, object> changes;

        public LinkedTemplatesModel linkedTemplates;
        public PublishedEnvironmentModel publishedEnvironments { get; set; }
    }
}
