using System;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Models.Other
{
    public class SearchSettingsModel
    {
        public string needle { get; set; }

        //Basic settings
        public Environments searchEnvironment { get; set; } = 0;

        //Template settings
        public Boolean searchTemplateId { get; set; } = true;
        public Boolean searchTemplateType { get; set; }
        public Boolean searchTemplateName{ get; set; }
        public Boolean searchTemplateData { get; set; }
        public Boolean searchTemplateParent { get; set; }
        public Boolean searchTemplateLinkedTemplates { get; set; }

        //Dynamic content settings
        public Boolean searchDynamicContentId { get; set; } = true;
        public Boolean searchDynamicContentFilledVariables { get; set; }
        public Boolean searchDynamicContentComponentName { get; set; }
        public Boolean searchDynamicContentComponentMode { get; set; }

        public Boolean IsTemplateSearchDisabled ()
        {
            if (searchTemplateId == false && searchTemplateType == false && searchTemplateName == false && searchTemplateData == false && searchTemplateParent == false && searchTemplateLinkedTemplates == false)
            {
                return true;
            } 
            return false;
        }
        public Boolean IsDynamicContentSearchDisabled()
        {
            if (searchDynamicContentId == false && searchDynamicContentFilledVariables == false && searchDynamicContentComponentName == false && searchDynamicContentComponentMode == false)
            {
                return true;
            }
            return false;
        }
    }
}
