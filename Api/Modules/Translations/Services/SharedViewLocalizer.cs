using System;
using System.Reflection;
using Api.Modules.Translations.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.Localization;

namespace Api.Modules.Translations.Services;

public class SharedViewLocalizer : ISharedViewLocalizer, ISingletonService
{
    private readonly IStringLocalizer _localizer;

    public SharedViewLocalizer(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("Index", Assembly.GetExecutingAssembly().GetName().Name);
    }

    public LocalizedString this[string key] => GetLocalizedString(key);

    public LocalizedString GetLocalizedString(string key)
    {
        LocalizedString s = _localizer[key];
        if (s.ToString() == key)
            Console.Out.WriteLine("LOCALIZATION ERROR: Unable to find translation for key '" + key + "'");
        else if (key.Contains(' ') || !key.Contains('_'))
            Console.Out.WriteLine("LOCALIZATION ERROR: Outdated key used '" + key + "'");
        return s;
    }

}