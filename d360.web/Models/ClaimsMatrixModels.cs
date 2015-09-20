using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class ClaimsMatrixEditorItemModel
    {
        public Claim Claim { get; set; }
        public ClaimObject ClaimObject { get; set; }
        public int? ID { get; set; }
    }

    public class ClaimsMatrixEditorModel
    {
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
        public int? ResponsibilityTypeID { get; set; }
        public List<ClaimsMatrixEditorItemModel> Items { get; set; }
    }
    public class ClaimsMatrixDisplayModel
    {
        public int ResponsibilityTypeID { get; set; }
        public List<ClaimsMatrixEditorItemModel> Items { get; set; }
    }
}