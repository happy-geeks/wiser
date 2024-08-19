namespace Api.Modules.Translations.Interfaces;
using Microsoft.Extensions.Localization;

public interface ISharedViewLocalizer
{
    public LocalizedString this[string key] { get; }
    LocalizedString GetLocalizedString(string key);
}