﻿using System;
using System.Collections.Generic;
using System.IO;
using FrontEnd.Core.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace FrontEnd.Core.Services;

public class WebPackService : IWebPackService
{
    private readonly IWebHostEnvironment webHostEnvironment;
    private Dictionary<string, string> Manifest { get; set; } = new();

    public WebPackService(IWebHostEnvironment webHostEnvironment)
    {
        this.webHostEnvironment = webHostEnvironment;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var webPackManifestLocation = Path.Combine(webHostEnvironment.WebRootPath, "scripts/manifest.json");
        if (!File.Exists(webPackManifestLocation))
        {
            throw new Exception($"Webpack manifest file not found at '{webPackManifestLocation}'");
        }

        var jsonText = await File.ReadAllTextAsync(webPackManifestLocation);
        if (String.IsNullOrWhiteSpace(jsonText))
        {
            throw new Exception($"Webpack manifest file is empty ({webPackManifestLocation})!");
        }

        Manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
    }

    /// <inheritdoc />
    public async Task<string> GetManifestFileAsync(string fileName)
    {
        if (webHostEnvironment.IsDevelopment())
        {
            // On development, always reload the manifest, so that we don't have to rebuild to test every javascript change.
            await InitializeAsync();
        }

        return Manifest.ContainsKey(fileName) ? Manifest[fileName] : null;
    }
}