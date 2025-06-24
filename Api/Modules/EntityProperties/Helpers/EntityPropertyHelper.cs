using System;
using Api.Modules.EntityProperties.Enums;

namespace Api.Modules.EntityProperties.Helpers;

#pragma warning disable CS1591
public class EntityPropertyHelper
#pragma warning restore CS1591
{
    /// <summary>
    /// Converts a string to its equivalent <see cref="EntityPropertyInputTypes"/> enum value.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>One of the values of <see cref="EntityPropertyInputTypes"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> doesn't contain a supported value.</exception>
    public static EntityPropertyInputTypes ToInputType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "input" => EntityPropertyInputTypes.Input,
            "secure-input" => EntityPropertyInputTypes.SecureInput,
            "textbox" => EntityPropertyInputTypes.TextBox,
            "radiobutton" => EntityPropertyInputTypes.RadioButton,
            "checkbox" => EntityPropertyInputTypes.CheckBox,
            "combobox" => EntityPropertyInputTypes.ComboBox,
            "multiselect" => EntityPropertyInputTypes.MultiSelect,
            "numeric-input" => EntityPropertyInputTypes.NumericInput,
            "file-upload" => EntityPropertyInputTypes.FileUpload,
            "htmleditor" => EntityPropertyInputTypes.HtmlEditor,
            "querybuilder" => EntityPropertyInputTypes.QueryBuilder,
            "date-time picker" => EntityPropertyInputTypes.DateTimePicker,
            "imagecoords" => EntityPropertyInputTypes.ImageCoordinates,
            "image-upload" => EntityPropertyInputTypes.ImageUpload,
            "gpslocation" => EntityPropertyInputTypes.GpsLocation,
            "daterange" => EntityPropertyInputTypes.DateRange,
            "sub-entities-grid" => EntityPropertyInputTypes.SubEntitiesGrid,
            "item-linker" => EntityPropertyInputTypes.ItemLinker,
            "color-picker" => EntityPropertyInputTypes.ColorPicker,
            "auto-increment" => EntityPropertyInputTypes.AutoIncrement,
            "linked-item" => EntityPropertyInputTypes.LinkedItem,
            "action-button" => EntityPropertyInputTypes.ActionButton,
            "data-selector" => EntityPropertyInputTypes.DataSelector,
            "chart" => EntityPropertyInputTypes.Chart,
            "scheduler" => EntityPropertyInputTypes.Scheduler,
            "timeline" => EntityPropertyInputTypes.TimeLine,
            "empty" => EntityPropertyInputTypes.Empty,
            "qr" => EntityPropertyInputTypes.Qr,
            "iframe" => EntityPropertyInputTypes.Iframe,
            "group" => EntityPropertyInputTypes.Group,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}