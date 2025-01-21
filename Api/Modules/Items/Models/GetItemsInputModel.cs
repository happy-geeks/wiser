using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Items.Models;

/// <summary>
/// The input for the get items endpoint of the API.
/// In here users can specify the title, entity type and details of the items they want to get.
/// They can also specify the page number and page size for paging.
/// </summary>
public class GetItemsInputModel
{
    /// <summary>
    /// The page number to get. Default value is 1.
    /// </summary>
    [Range(1, Int32.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// The amount of items per page to get. The default value is 100, maximum value is 500.
    /// </summary>
    [Range(1, 500)]
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// By default, the API will use the internal property names for the details, such as "long_description".
    /// If you don't know these values and/or want to use more friendly names, you can set this to true, so that you can use the display name of the field, such as "Long Description".
    /// </summary>
    public bool UseFriendlyPropertyNames { get; set; } = true;

    /// <summary>
    /// If you want to get an item that has a specific title, you can use this filter.
    /// This is an exact match filter, but it is case-insensitive.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// If you want to get items that have a specific entity type, you can use this filter.
    /// In most cases you'll want to have at least a filter on entity type, otherwise you'll get all items that you have access too.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Filters for the details of the items.
    /// </summary>
    [FromQuery]
    public List<WiserItemDetailInputModel> Details { get; set; } = [];
}