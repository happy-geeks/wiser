using System;
using JetBrains.Annotations;

namespace FrontEnd.Core.Models
{
    public class BaseViewModel
    {
        public FrontEndSettings Settings { get; set; }

        [CanBeNull] public Version WiserVersion { get; set; }

        public string SubDomain { get; set; }

        public bool IsTestEnvironment { get; set; }

        public string Wiser1BaseUrl { get; set; }

        public bool LoadPartnerStyle { get; set; }

        public string ApiAuthenticationUrl { get; set; }

        public string ApiRoot { get; set; }

        public string CurrentDomain { get; set; }

        public string IsWiserFrontEndLogin { get; set; }
    }
}