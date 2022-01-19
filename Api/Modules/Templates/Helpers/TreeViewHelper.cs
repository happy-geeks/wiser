using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Helpers
{
    public class TreeViewHelper
    {
        /// <summary>
        /// Converts a TemplateTreeViewDAO to a TemplateTreeViewModel
        /// </summary>
        /// <param name="rawTreeView">The raw data in the form of a TemplateTreeViewDAO to convert.</param>
        /// <returns>A TemplateTreeViewModel containing the data provided in the param</returns>
        public TemplateTreeViewModel ConvertTemplateTreeViewDAOToTemplateTreeViewModel (TemplateTreeViewDAO rawTreeView)
        {
            var treeViewModel = new TemplateTreeViewModel(
                rawTreeView.GetTemplateId(),
                rawTreeView.GetTemplateName(),
                (rawTreeView.GetTemplateType() == (int)TreeViewTypeEnum.folder || rawTreeView.GetTemplateType() == (int)TreeViewTypeEnum.root),
                rawTreeView.GetHasChildren()
            );

            return treeViewModel;
        }
    }
}
