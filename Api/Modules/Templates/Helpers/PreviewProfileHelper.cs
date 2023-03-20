using System.Collections.Generic;
using Api.Modules.Templates.Models.Preview;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Helpers
{
    /// <summary>
    /// Helper class for a PreviewProfile
    /// </summary>
    public class PreviewProfileHelper
    {
        /// <summary>
        /// Converts a PreviewProfileDAO to a PreviewProfileModel.
        /// </summary>
        /// <param name="previewDAO">A DAO containing the data of a preview profile</param>
        /// <returns>A preview profile model containing the data of the supplied dao</returns>
        public static PreviewProfileModel ConvertPreviewProfileDAOToModel (PreviewProfileDao previewDAO)
        {
            var previewModel = new PreviewProfileModel
            {
                Id = previewDAO.Id,
                Name = previewDAO.Name,
                Url = previewDAO.Url,
                Variables = JsonConvert.DeserializeObject<List<PreviewVariableModel>>(previewDAO.RawVariables)
            };

            return previewModel;
        }

        /// <summary>
        /// Converts a PreviewProfileModel to a PreviewProfileDAO
        /// </summary>
        /// <param name="previewModel">A PreviewProfileModel containing the display data of a preview profile.</param>
        /// <returns>A PreviewProfileDao containing the data of the supplied dao</returns>
        public static PreviewProfileDao ConvertPreviewProfileModelToDAO(PreviewProfileModel previewModel)
        {
            var previewDAO = new PreviewProfileDao(
                    previewModel.Id,
                    previewModel.Name,
                    previewModel.Url,
                    JsonConvert.SerializeObject(previewModel.Variables)
                );

            return previewDAO;
        }
    }
}
