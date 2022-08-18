using System;
using System.Collections.Generic;

namespace Api.Modules.CloudFlare.Models
{
    public class DirectUploadResult
    {
        public string Id { get; set; }
        public string UploadURL { get; set; }
    }

    public class DirectUploadResponseModel
    {
        public DirectUploadResult Result { get; set; }
        public object ResultInfo { get; set; }
        public bool Success { get; set; }
        public List<object> Errors { get; set; }
        public List<object> Messages { get; set; }
    }

}

