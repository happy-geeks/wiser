using System.Collections.Generic;
using Api.Modules.Templates.Models.Preview;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Helpers
{
    public class PreviewProfileHelper
    {
        /// <summary>
        /// Converts a PreviewProfileDAO to a PreviewProfileModel.
        /// </summary>
        /// <param name="previewDAO">A DAO containing the data of a preview profile</param>
        /// <returns>A preview profile model containing the data of the supplied dao</returns>
        public static PreviewProfileModel ConvertPreviewProfileDAOToModel (PreviewProfileDAO previewDAO)
        {
            var previewModel = new PreviewProfileModel
            {
                id = previewDAO.GetId(),
                name = previewDAO.GetName(),
                url = previewDAO.GetUrl(),
                variables = JsonConvert.DeserializeObject<List<PreviewVariableModel>>(previewDAO.GetRawVariables())
            };

            return previewModel;
        }

        /// <summary>
        /// Converts a PreviewProfileModel to a PreviewProfileDAO
        /// </summary>
        /// <param name="previewModel">A PreviewProfileModel containing the display data of a preview profile.</param>
        /// <returns>A PreviewProfileDao containing the data of the supplied dao</returns>
        public static PreviewProfileDAO ConvertPreviewProfileModelToDAO(PreviewProfileModel previewModel)
        {
            var previewDAO = new PreviewProfileDAO(
                    previewModel.id,
                    previewModel.name,
                    previewModel.url,
                    JsonConvert.SerializeObject(previewModel.variables)
                );

            return previewDAO;
        }
    }
}
