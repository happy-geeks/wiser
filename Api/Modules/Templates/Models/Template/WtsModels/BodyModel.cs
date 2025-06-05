using System.Collections.Generic;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class BodyModel
{   
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "ContentType",
        UseDataSource = true,
        Description = "Het gewenste contenttype.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string ContentType { get; set; }
    
    [WtsProperty(
        IsVisible = true,
        Title = "Bodies",
        Description = "",
        ConfigurationTab = ConfigurationTab.HttpApis,
        DataComponent = DataComponents.KendoGrid,
        AllowEdit = true,
        IdProperty = "uid",
        UseDataSource = true,
        KendoOptions = @"
           {
              ""resizable"": true,
              ""height"": 280,
              ""selectable"": true,
              ""columns"": [
                {
                    ""field"": ""text"",
                    ""title"": ""Body part text""
                }
              ]
           }
        "
    )]
    public List<BodyPartModel> BodyParts { get; set; }
}