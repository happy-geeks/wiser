using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using static GeeksCoreLibrary.Core.Cms.Attributes.CmsAttributes;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="IDynamicContentService" />
    public class DynamicContentService : IDynamicContentService, IScopedService
    {
        private readonly IDynamicContentDataService dataService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentService"/>.
        /// </summary>
        public DynamicContentService(IDynamicContentDataService dataService)
        {
            this.dataService = dataService;
        }

        /// <inheritdoc />
        public Dictionary<int, string> GetComponentModes(Type component)
        {
            var info = (component.BaseType).GetTypeInfo();
            var enumtype = info.GetGenericArguments()[1];
            var enumFields = enumtype.GetFields();

            var returnDict = new Dictionary<int, string>();

            foreach (var enumField in enumFields)
            {
                if (enumField.Name.Equals("value__")) continue;
                returnDict.Add((int)enumField.GetRawConstantValue(), enumField.Name);
            }

            return returnDict;
        }
        
        /// <inheritdoc />
        public ServiceResult<List<ComponentModeModel>> GetComponentModes(string componentType)
        {
            if (String.IsNullOrWhiteSpace(componentType))
            {
                return new ServiceResult<List<ComponentModeModel>>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Name cannot be empty"
                };
            }
            
            var type = ReflectionHelper.GetComponentTypeByName(componentType);
            if (type == null)
            {
                return new ServiceResult<List<ComponentModeModel>>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Component with type '{componentType}' not found."
                };
            }

            var componentModes = GetComponentModes(type);
            var results = componentModes.Select(componentMode => new ComponentModeModel { Id = componentMode.Key, Name = componentMode.Value }).ToList();

            return new ServiceResult<List<ComponentModeModel>>(results);
        }

        /// <inheritdoc />
        public List<PropertyInfo> GetPropertiesOfType(Type CmsSettingsType)
        {
            var resultlist = new List<PropertyInfo>();
            var allProperties = CmsSettingsType.GetProperties();

            foreach (var property in allProperties)
            {
                resultlist.Add(property);
            }

            return resultlist;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<string, object>>> GetComponentDataAsync(int contentId)
        {
            var data = (await dataService.GetComponentDataAsync(contentId)).Value;
            
            if (data == null)
            {
                return new ServiceResult<Dictionary<string, object>>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }
            
            return new ServiceResult<Dictionary<string, object>>(data);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> SaveNewSettingsAsync(ClaimsIdentity identity, int contentId, string component, int componentMode, string title, Dictionary<string, object> settings)
        {
            var modes = GetComponentModes(ReflectionHelper.GetComponentTypeByName(component));
            modes.TryGetValue(componentMode, out var componentModeName);
            return new ServiceResult<int>(await dataService.SaveSettingsStringAsync(contentId, component, componentModeName, title, settings, IdentityHelpers.GetUserName(identity)));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DynamicContentOverviewModel>> GetMetaDataAsync(int contentId)
        {
            var result = await dataService.GetMetaData(contentId);
            if (result == null)
            {
                return new ServiceResult<DynamicContentOverviewModel>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            result.Data = (await dataService.GetComponentDataAsync(contentId)).Value;
            return new ServiceResult<DynamicContentOverviewModel>
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = result
            };
        }
    }
}
