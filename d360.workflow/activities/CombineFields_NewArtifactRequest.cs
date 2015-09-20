using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.core.entities;
using System.Data.SqlClient;
using d360.core;
using d360.utils.company;
using Dapper;
using d360.workflow.models;
using System.Activities.Tracking;
using d360.core.enums;
using System.Xml.Linq;

namespace d360.workflow
{
    public sealed class CombineFields_NewArtifactRequest : CodeActivity
    {
        public InArgument<NewArtifactRequest> RequestInfo { get; set; }
        public OutArgument<XElement> RequestFields { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var r = context.GetValue(this.RequestInfo);

            var xml = r.ToXml();

            if (r.ParentID.HasValue)
            {
                xml.Add(new XElement("ParentID", r.ParentID));
            }

            if (r.Fields != null)
            {
                foreach (var k in r.Fields.Keys)
                {
                    xml.Add(new XElement(k.ToString(), r.Fields[k]));
                }            
            }

            context.SetValue<XElement>(this.RequestFields, xml);
        }
    }
}
