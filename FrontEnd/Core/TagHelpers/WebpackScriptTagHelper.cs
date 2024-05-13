using System;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace FrontEnd.Core.TagHelpers;

[HtmlTargetElement("webpack-script")]  
public class WebpackScriptTagHelper : TagHelper  
{
    /// <summary>
    /// Gets or sets the file name of the javascript file that should be loaded from the Webpack manifest.
    /// </summary>
    public string FileName { get; set; }
    
    private readonly IWebPackService webPackService;

    /// <summary>
    /// Creates a new instance of <see cref="WebpackScriptTagHelper"/>.
    /// </summary>
    public WebpackScriptTagHelper(IWebPackService webPackService)
    {
        this.webPackService = webPackService;
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Get the full script location (including anti-caching-hash) from the Webpack manifest. 
        var scriptLocation = await webPackService.GetManifestFileAsync(FileName);
        if (String.IsNullOrWhiteSpace(scriptLocation))
        {
            // If the file name doesn't exist in the Webpack manifest, then don't render a script tag for it. 
            output.SuppressOutput();
            return;
        }

        // Add the javascript to the page.
        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Add(new TagHelperAttribute("defer"));
        output.Attributes.SetAttribute("src", scriptLocation);
    }
}