using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Attributes
{
    public class WtsAttributes
    {
        [AttributeUsage(AttributeTargets.Property)]
        public class WtsPropertyAttribute : Attribute
        {
            public bool isVisible { get; set; }
            public string Description { get; set; }
            public string Title { get; set; }
            public string KendoTab { get; set; }
            // public KendoTab KendoTab { get; set; }
            public KendoComponent KendoComponent { get; set; }
            public string KendoOptions { get; set; }
            public string BelongsToForm { get; set; }
            public bool isFilled { get; set; }
        }
    }
}