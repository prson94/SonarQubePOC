using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models
{
    /// <summary>
    /// Serves as the base editor model for all forms.
    /// </summary>
    public class EditorModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Uri { get; set; }
        public string Method { get; set; }
        public string Context { get; set; }

        public bool HasPermission { get; set; }
    }

    public class IntersectTypeEditorModel: EditorModel
    {
        public int ID { get; set; }

        public bool ReadOnly { get; set; }
        public bool IsTechnical { get; set; }
        public bool AllowGrouping { get; set; }
        public bool AllowSourcing { get; set; }

        public int Side1ID { get; set; }
        public int Side1Order { get; set; }
        public bool Side1IsSourcingItem { get; set; }
        public string Side1DisplayText { get; set; }
        public List<SelectListItem> Side1Options { get; set; }
        public List<SelectListItem> Order1Options { get; set; }

        public int Side2ID { get; set; }
        public int Side2Order { get; set; }
        public bool Side2IsSourcingItem { get; set; }
        public string Side2DisplayText { get; set; }
        public List<SelectListItem> Side2Options { get; set; }
        public List<SelectListItem> Order2Options { get; set; }

        /// <summary>
        /// Should certain fields be made read-only based on whether any 
        /// relationships exist for this type.
        /// </summary>
        public bool LimitedChangesOnly { get; set; }
    }
}