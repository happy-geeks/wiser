using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.History;
using FrontEnd.Modules.Templates.Interfaces;
using FrontEnd.Modules.Templates.Models;
using GeeksCoreLibrary.Core.Cms.Attributes;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FrontEnd.Modules.Templates.Services;

public class FrontEndDynamicContentService : IFrontEndDynamicContentService
{
    /// <inheritdoc />
    public DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, Dictionary<string, object> data, string componentMode)
    {
        var cmsSettingsType = ReflectionHelper.GetCmsSettingsType(component);
        return GenerateDynamicContentInformationViewModel(component, cmsSettingsType.GetProperties(), data, componentMode);
    }

    /// <inheritdoc />
    public DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, IEnumerable<PropertyInfo> properties, Dictionary<string, object> data, string componentMode)
    {
        var result = new DynamicContentInformationViewModel { Tabs = []};
        var orderedProperties = properties.Where(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>() != null).OrderBy(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>()?.DisplayOrder ?? 999);

        foreach (var property in orderedProperties)
        {
            var cmsPropertyAttribute = property.GetCustomAttribute<CmsPropertyAttribute>();

            if (cmsPropertyAttribute == null || cmsPropertyAttribute.HideInCms)
            {
                continue;
            }

            var tabEnumAttribute = cmsPropertyAttribute.TabName.GetAttributeOfType<CmsEnumAttribute>();
            var tab = result.Tabs.SingleOrDefault(t => t.Name == cmsPropertyAttribute.TabName);
            if (tab == null)
            {
                tab = new TabViewModel
                {
                    Name = cmsPropertyAttribute.TabName,
                    PrettyName = tabEnumAttribute?.PrettyName ?? cmsPropertyAttribute.TabName.ToString(),
                    Groups = []
                };

                result.Tabs.Add(tab);
            }

            var groupEnumAttribute = cmsPropertyAttribute.GroupName.GetAttributeOfType<CmsEnumAttribute>();
            var group = tab.Groups.SingleOrDefault(g => g.Name == cmsPropertyAttribute.GroupName);
            if (group == null)
            {
                group = new GroupViewModel
                {
                    Name = cmsPropertyAttribute.GroupName,
                    PrettyName = groupEnumAttribute?.PrettyName ?? cmsPropertyAttribute.GroupName.ToString(),
                    Fields = []
                };

                tab.Groups.Add(group);
            }

            var isDefaultValue = false;
            var value = data.FirstOrDefault(setting => property.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase)).Value;

            // If we have no value, get the default value.
            if (component != null && (value == null || Equals(value, property.PropertyType.GetDefaultValue()) || (value is string stringValue && String.IsNullOrEmpty(stringValue))))
            {
                var assembly = Assembly.GetAssembly(component);
                var fullTypeName = $"{component.Namespace}.Models.{component.Name}{componentMode}SettingsModel";
                var type = assembly?.GetType(fullTypeName);
                var defaultValueProperty = type?.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(p => p.Name == property.Name);
                var defaultValueAttribute = defaultValueProperty?.GetCustomAttribute<DefaultValueAttribute>();
                object defaultValue;
                if (defaultValueAttribute != null)
                {
                    defaultValue = defaultValueAttribute.Value;
                    isDefaultValue = true;
                }
                else
                {
                    defaultValue = property.GetValue(Activator.CreateInstance(property.DeclaringType));
                    isDefaultValue = value != null && (value is not string v || !String.IsNullOrEmpty(v));
                }

                // Do not force the default value if the type of value is a value type, as it might incorrectly overwrite the value.
                // Say, for example, that the default value of a bool property is True, but a developer has set that property to False, the final value would be True.
                // Same goes for integers: if the default value of that property is 100, but a developer has set it to 0, the final value would be 100.
                if (value == null || !value.GetType().IsValueType)
                {
                    value = defaultValue;
                }
            }

            var field = new FieldViewModel
            {
                Name = property.Name,
                PrettyName = String.IsNullOrEmpty(cmsPropertyAttribute.PrettyName) ? property.Name : cmsPropertyAttribute.PrettyName,
                CmsPropertyAttribute = cmsPropertyAttribute,
                PropertyInfo = property,
                Value = value,
                IsDefaultValue = isDefaultValue
            };
            group.Fields.Add(field);

            // Get sub values if we have any. (For example, a Repeater component has a dynamic amount of layers, this code handles that functionality.)
            if (property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(SortedList<,>) || property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && property.PropertyType.GetGenericArguments().First() == typeof(string))
            {
                var subSettingsType = property.PropertyType.GetGenericArguments().Last();

                var subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), new Dictionary<string, object>(), componentMode);
                var subFields = subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList();
                subFields.ForEach(f => f.IsSubField = true);

                field.SubFields = new Dictionary<string, List<FieldViewModel>>
                {
                    { "_template", subFields }
                };

                if (value is JObject { HasValues: true } jsonObject)
                {
                    foreach (var item in jsonObject.Properties())
                    {
                        subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), item.Value.ToObject<Dictionary<string, object>>(), componentMode);
                        subFields = subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList();
                        subFields.ForEach(f => f.IsSubField = true);
                        field.SubFields ??= new Dictionary<string, List<FieldViewModel>>();
                        field.SubFields.Add(item.Name, subFields);
                    }
                }
                else
                {
                    subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), new Dictionary<string, object>(), componentMode);
                    subFields = subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList();
                    subFields.ForEach(f => f.IsSubField = true);
                    field.SubFields.Add("", subFields);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public List<(FieldViewModel OldVersion, FieldViewModel NewVersion)> GenerateChangesListForHistory(List<DynamicContentChangeModel> dynamicContentChanges)
    {
        var results = new List<(FieldViewModel OldVersion, FieldViewModel NewVersion)>();
        if (dynamicContentChanges == null)
        {
            return results;
        }

        var componentFields = new Dictionary<string, List<FieldViewModel>>();

        foreach (var historyChange in dynamicContentChanges)
        {
            if (!componentFields.ContainsKey(historyChange.Component))
            {
                var componentType = ReflectionHelper.GetComponentTypeByName(historyChange.Component);
                var dynamicContentInformation = GenerateDynamicContentInformationViewModel(componentType, new Dictionary<string, object>(), historyChange.ComponentMode);
                componentFields.Add(historyChange.Component, dynamicContentInformation.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList());
            }

            var field = componentFields[historyChange.Component].SingleOrDefault(f => String.Equals(f.Name, historyChange.Property, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                continue;
            }

            var oldModel = new FieldViewModel
            {
                Value = historyChange.OldValue,
                CmsPropertyAttribute = field.CmsPropertyAttribute,
                Name = field.Name,
                PrettyName = String.IsNullOrEmpty(field.CmsPropertyAttribute.PrettyName) ? field.PropertyInfo.Name : field.CmsPropertyAttribute.PrettyName,
                PropertyInfo = field.PropertyInfo
            };

            var newModel = new FieldViewModel
            {
                Value = historyChange.NewValue,
                CmsPropertyAttribute = field.CmsPropertyAttribute,
                Name = field.Name,
                PrettyName = String.IsNullOrEmpty(field.CmsPropertyAttribute.PrettyName) ? field.PropertyInfo.Name : field.CmsPropertyAttribute.PrettyName,
                PropertyInfo = field.PropertyInfo
            };

            var oldValueJObject = historyChange.OldValue as JObject;
            var newValueJObject = historyChange.NewValue as JObject;

            if (oldValueJObject != null || newValueJObject != null)
            {
                var amountOfGroups = Math.Max(oldValueJObject == null ? 0 : oldValueJObject.Properties().Count(), newValueJObject == null ? 0 : newValueJObject.Properties().Count());
                for (var index = 0; index < amountOfGroups; index++)
                {
                    var groupKey = index == 0 ? "" : $"id{index}";
                    oldModel.SubFields ??= new Dictionary<string, List<FieldViewModel>>();
                    newModel.SubFields ??= new Dictionary<string, List<FieldViewModel>>();

                    oldModel.SubFields.Add(groupKey, []);
                    newModel.SubFields.Add(groupKey, []);

                    var oldValueGroup = (JObject)oldValueJObject?[groupKey] ?? new JObject();
                    var newValueGroup = (JObject)newValueJObject?[groupKey] ?? new JObject();

                    foreach (var subField in field.SubFields.First().Value)
                    {
                        var oldValue = ((JValue)oldValueGroup.GetValue(subField.Name))?.Value ?? "";
                        var newValue = ((JValue)newValueGroup.GetValue(subField.Name))?.Value ?? "";
                        if (oldValue.ToString() == newValue.ToString())
                        {
                            continue;
                        }

                        var prettyName = String.IsNullOrEmpty(subField.CmsPropertyAttribute.PrettyName) ? subField.PropertyInfo.Name : subField.CmsPropertyAttribute.PrettyName;

                        oldModel.SubFields[groupKey].Add(new FieldViewModel
                        {
                            Value = oldValue,
                            CmsPropertyAttribute = subField.CmsPropertyAttribute,
                            Name = subField.Name,
                            PrettyName = $"{prettyName} - Layer {index}",
                            PropertyInfo = subField.PropertyInfo,
                            IsSubField = true
                        });

                        newModel.SubFields[groupKey].Add(new FieldViewModel
                        {
                            Value = newValue,
                            CmsPropertyAttribute = subField.CmsPropertyAttribute,
                            Name = subField.Name,
                            PrettyName = $"{prettyName} - Layer {index}",
                            PropertyInfo = subField.PropertyInfo,
                            IsSubField = true
                        });
                    }
                }
            }

            results.Add((oldModel, newModel));
        }

        return results;
    }
}