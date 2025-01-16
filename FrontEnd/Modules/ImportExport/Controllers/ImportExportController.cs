using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.ImportExport.Interfaces;
using FrontEnd.Modules.ImportExport.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.ImportExport.Controllers;

[Area("ImportExport"), Route("Modules/ImportExport")]
public class ImportExportController(IBaseService baseService, IWebHostEnvironment webHostEnvironment, IImportsService importsService)
    : Controller
{
    public IActionResult Index()
    {
        return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
    }

    [HttpGet, Route("Import")]
    public IActionResult Import()
    {
        return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
    }

    [HttpGet, Route("Import/Html")]
    public IActionResult ImportHtml()
    {
        return View();
    }

    [HttpPost, Route("Import/Upload")]
    public async Task<IActionResult> UploadAsync([FromQuery]string type = null)
    {
        var uploadsDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "temp/import/uploads");
        if (!Directory.Exists(uploadsDirectory))
        {
            Directory.CreateDirectory(uploadsDirectory);
        }

        var formCollection = await Request.ReadFormAsync();
        if (!formCollection.Files.Any())
        {
            return new JsonResult(new FeedFileUploadResultModel { Successful = false });
        }

        var fileType = type ?? "feed";

        return fileType switch
        {
            "feed" => new JsonResult(await importsService.HandleFeedFileUploadAsync(formCollection, uploadsDirectory)),
            "images" => new JsonResult(await importsService.HandleImagesFileUploadAsync(formCollection, uploadsDirectory)),
            _ => new JsonResult(new FeedFileUploadResultModel { Successful = false })
        };
    }

    [HttpPost, Route("Import/Delete")]
    public void DeleteTemporaryFiles(string fileNames)
    {
        var uploadsDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "temp/import/uploads");
        if (!Directory.Exists(uploadsDirectory) || String.IsNullOrWhiteSpace(fileNames))
        {
            return;
        }

        foreach (var fileName in fileNames.Split(','))
        {
            var location = Path.Combine(uploadsDirectory, fileName);
            if (!System.IO.File.Exists(location))
            {
                continue;
            }

            System.IO.File.Delete(location);
        }
    }

    [HttpGet, Route("Export")]
    public IActionResult Export()
    {
        return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
    }

    [HttpGet, Route("Export/Html")]
    public IActionResult ExportHtml()
    {
        return View();
    }
}