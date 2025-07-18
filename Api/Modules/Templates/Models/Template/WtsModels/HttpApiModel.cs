using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

[XmlType("HttpApi")]
public class HttpApiModel : ActionModel
{
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        UseDataSource = true,
        Title = "URL",
        Description = "URL voor de http request.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string Url { get; set; }

    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Http method",
        Description = "Welke HTTP methode moet er worden gebruikt?",
        //ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoDropDownList
    )]
    public HttpMethod Method { get; set; }

    public string OAuth { get; set; }//TODO: seemingly not present in documentation ask about how this one works/examples later

    [XmlIgnore]
    private bool? singleRequest;

    [WtsProperty(
        IsVisible = true,
        Title = "Single request",
        Description = "Als het geen single request is, wordt er een array gemaakt voor de resultset.",
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoCheckBox
    )]
    public bool? SingleRequest
    {
        get
        {
            if (singleRequest == null || singleRequest.Value == false)
            {
                return null;
            }
            return singleRequest;
        }
        set => singleRequest = value;
    }

    [XmlIgnore]
    private bool? ignoreSslValidationErrors;
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        Title = "Negeer SSL-validatiefouten.",
        Description = "Sta toe dat er verbindingen gemaakt worden, ook als er geen goed SSL-certificaat is.",
        //ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoCheckBox
    )]

    public bool? IgnoreSslValidationErrors
    {
        get
        {
            if (ignoreSslValidationErrors == null || ignoreSslValidationErrors.Value == false)
            {
                return null;
            }
            return ignoreSslValidationErrors;
        }
        set => ignoreSslValidationErrors = value;
    }

    [XmlElement("IgnoreSslValidation")]
    [CanBeNull]
    public string IgnoreSslValidationErrorsString
    {
        get
        {
            if (ignoreSslValidationErrors == null || ignoreSslValidationErrors.Value==false)
            {
                return null;
            }
            return ignoreSslValidationErrors.ToString();
        }
        set
        {
            Boolean.TryParse(value, out var valid);
            if (!valid)
            {
                ignoreSslValidationErrors = false;
                return;
            }
            ignoreSslValidationErrors = Boolean.Parse(value);
        }
    }

    private string nextUrlProperty;

    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Next url",
        Description = "Url voor de volgende pagina van het resultaat.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string NextUrlProperty {
        get
        {
            if (String.IsNullOrWhiteSpace(nextUrlProperty))
            {
                return null;
            }
            return nextUrlProperty;
        } set=>nextUrlProperty = value; }

    private List<Header> headers { get; set; } = [];

    [WtsProperty(
        IsVisible = true,
        Title = "Headers",
        Description = "",
        ConfigurationTab = ConfigurationTab.HttpApis,
        DataComponent = DataComponents.KendoGrid,
        AllowEdit = true,
        IdProperty = "ID",
        UseDataSource = true,
        KendoOptions = @"
           {
              ""resizable"": true,
              ""height"": 280,
              ""selectable"": true,
              ""columns"": [
                {
                    ""field"": ""name"",
                    ""title"": ""Name""
                }
              ]
           }
        "
    )]
    public List<Header> Headers
    {
        get => headers;
        set => headers = value;
    }

    [XmlIgnore]
    [CanBeNull]
    private string timeout;

    /// <summary>
    /// Gets or sets the timeout in seconds. If the integer is 0 it will be null instead.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Time out",
        Description = "Hoeveel seconden tot de time-out? 0 betekent geen time out.",
        DataComponent = DataComponents.KendoNumericTextBox,
        KendoOptions = @"
                   {
                      ""format"": ""#"",
                      ""decimals"": 0
                    }
                "
    )]
    [CanBeNull]
    public string Timeout {
        get
        {
            if(timeout == "0")
            {
                return null;
            }

            return timeout;
        }
        set => timeout = value;
    }

    public BodyModel Body { get; set; }=new BodyModel();

    public string ResultContentType { get; set; }
}