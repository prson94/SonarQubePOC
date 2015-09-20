using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class FusionPromotionRuleEditorModel
    {
        public bool IsUsed { get; set; }

        public int FusionTypeID { get; set; }

        public int FusionID { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FusionAttributePromotionRule Rule { get; set; }
    }
}