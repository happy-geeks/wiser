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
        public Boolean SearchTemplateId { get; set; } = true;
        public Boolean SearchTemplateType { get; set; }
        public Boolean SearchTemplateName{ get; set; }
        public Boolean SearchTemplateData { get; set; }
        public Boolean SearchTemplateParent { get; set; }
        public Boolean SearchTemplateLinkedTemplates { get; set; }

        //Dynamic content settings
        public Boolean SearchDynamicContentId { get; set; } = true;
        public Boolean SearchDynamicContentFilledVariables { get; set; }
        public Boolean SearchDynamicContentComponentName { get; set; }
        public Boolean SearchDynamicContentComponentMode { get; set; }

        public Boolean IsTemplateSearchDisabled ()
        {
            if (SearchTemplateId == false && SearchTemplateType == false && SearchTemplateName == false && SearchTemplateData == false && SearchTemplateParent == false && SearchTemplateLinkedTemplates == false)
            {
                return true;
            } 
            return false;
        }
        public Boolean IsDynamicContentSearchDisabled()
        {
            if (SearchDynamicContentId == false && SearchDynamicContentFilledVariables == false && SearchDynamicContentComponentName == false && SearchDynamicContentComponentMode == false)
            {
                return true;
            }
            return false;
        }
    }
}
