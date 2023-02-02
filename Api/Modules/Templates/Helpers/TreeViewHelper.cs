using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Helpers
{
#pragma warning disable CS1591
    public class TreeViewHelper
#pragma warning restore CS1591
    {
        /// <summary>
        /// Converts a TemplateTreeViewDAO to a TemplateTreeViewModel
        /// </summary>
        /// <param name="rawTreeView">The raw data in the form of a TemplateTreeViewDAO to convert.</param>
        /// <returns>A TemplateTreeViewModel containing the data provided in the param</returns>
        public TemplateTreeViewModel ConvertTemplateTreeViewDAOToTemplateTreeViewModel (TemplateTreeViewDao rawTreeView)
        {
            var treeViewModel = new TemplateTreeViewModel
            {
                TemplateId = rawTreeView.TemplateId,
                TemplateName = rawTreeView.TemplateName,
                IsFolder = rawTreeView.TemplateType == TemplateTypes.Directory,
                HasChildren = rawTreeView.HasChildren,
                TemplateType = (int)rawTreeView.TemplateType,
                IsVirtualItem = rawTreeView.IsVirtualItem
            };

            return treeViewModel;
        }
    }
}
