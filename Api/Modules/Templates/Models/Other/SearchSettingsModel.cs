using System;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Models.Other
{
    public class SearchSettingsModel
    {
        public string Needle { get; set; }

        //Basic settings
        public Environments SearchEnvironment { get; set; } = 0;

        //Template settings
        public bool SearchTemplateId { get; set; } = true;
        public bool SearchTemplateType { get; set; }
        public bool SearchTemplateName { get; set; }
        public bool SearchTemplateData { get; set; }
        public bool SearchTemplateParent { get; set; }
        public bool SearchTemplateLinkedTemplates { get; set; }

        //Dynamic content settings
        public bool SearchDynamicContentId { get; set; } = true;
        public bool SearchDynamicContentSettings { get; set; }
        public bool SearchDynamicContentComponentName { get; set; }
        public bool SearchDynamicContentComponentMode { get; set; }

        public bool IsTemplateSearchDisabled ()
        {
            if (SearchTemplateId == false && SearchTemplateType == false && SearchTemplateName == false && SearchTemplateData == false && SearchTemplateParent == false && SearchTemplateLinkedTemplates == false)
            {
                return true;
            } 
            return false;
        }
        public bool IsDynamicContentSearchDisabled()
        {
            if (SearchDynamicContentId == false && SearchDynamicContentSettings == false && SearchDynamicContentComponentName == false && SearchDynamicContentComponentMode == false)
            {
                return true;
            }
            return false;
        }
    }
}
