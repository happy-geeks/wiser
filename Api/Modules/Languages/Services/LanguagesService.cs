using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;

namespace Api.Modules.Languages.Services;

/// <inheritdoc cref="ILanguagesService" />
public class LanguagesService : ILanguagesService, IScopedService
{
    private readonly GeeksCoreLibrary.Modules.Languages.Interfaces.ILanguagesService gclLanguagesService;

    /// <summary>
    /// Creates a new instance of <see cref="LanguagesService"/>.
    /// </summary>
    public LanguagesService(GeeksCoreLibrary.Modules.Languages.Interfaces.ILanguagesService gclLanguagesService)
    {
        this.gclLanguagesService = gclLanguagesService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<LanguageModel>>> GetAllLanguagesAsync()
    {
        var results = await gclLanguagesService.GetAllLanguagesAsync();
        return new ServiceResult<List<LanguageModel>>(results);
    }
}