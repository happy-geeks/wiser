using System;
using System.Collections.Generic;

namespace Api.Modules.CloudFlare.Models
{
    public class UploadImageReponseModel
    {
        public bool Success { get; set; }
        public List<object> Errors { get; set; }
        public List<object> Messages { get; set; }
        public List<Result> Result { get; set; }

    }
    public class Metadata
    {
        public string Meta { get; set; }
    }

    public class Result
    {
        public string Id { get; set; }
        public string Filename { get; set; }
        public Metadata Metadata { get; set; }
        public bool RequireSignedURLs { get; set; }
        public Variants Variants { get; set; }
        public DateTime Uploaded { get; set; }
    }

    public class Variants
    {
        public string Thumbnail { get; set; }
        public string Hero { get; set; }
        public string Original { get; set; }
    }

}

