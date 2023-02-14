using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Helpers
{
    /// <summary>
    /// A helper class for functions to do something with tree views in the template module.
    /// </summary>
    public class TreeViewHelper
    {
        /// <summary>
        /// Converts a TemplateTreeViewDAO to a TemplateTreeViewModel
        /// </summary>
        /// <param name="rawTreeView">The raw data in the form of a TemplateTreeViewDAO to convert.</param>
        /// <returns>A TemplateTreeViewModel containing the data provided in the param</returns>
        public static TemplateTreeViewModel ConvertTemplateTreeViewDaoToTemplateTreeViewModel(TemplateTreeViewDao rawTreeView)
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
