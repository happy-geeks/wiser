using System;
using System.Collections.Generic;

namespace Api.Modules.CloudFlare.Models
{
    public class UploadImageResult
    {
        public string Id { get; set; }
        public string Filename { get; set; }
        public DateTime Uploaded { get; set; }
        public bool RequireSignedURLs { get; set; }
        public List<string> Variants { get; set; }
    }

    public class UploadImageResponseModel
    {
        public UploadImageResult Result { get; set; }
        public object ResultInfo { get; set; }
        public bool Success { get; set; }
        public List<object> Errors { get; set; }
        public List<object> Messages { get; set; }

    }
}
