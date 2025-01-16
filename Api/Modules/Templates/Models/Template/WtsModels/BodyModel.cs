using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class BodyModel
{
    public string ContentType { get; set; }

    public List<BodyPartModel> BodyParts { get; set; }
}