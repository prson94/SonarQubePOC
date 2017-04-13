using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormModel
    {
        

        public List<WorkflowFormFormModel> Forms { get; set; }

        public WorkflowFormSettingsModel Settings { get; set; }

        public static WorkflowFormModel ParseXml(XElement xml)
        {
            if (xml == null) throw new Exception("INVALID XML SPECIFIED FOR FORM MODEL");

            var model = new WorkflowFormModel();
            model.Forms = new List<WorkflowFormFormModel>();

            var forms = xml.Elements("form");

            if (forms == null) return model;

            foreach (var form in forms)
            {
                model.Forms.Add(WorkflowFormFormModel.ParseXml(form));                
            }

            return model;
        }

        public string GetFormValueById(string id)
        {
            var form = Forms.FirstOrDefault();

            if (form == null) return "";

            var res = form.Fields.Where(x => x.ID == id).FirstOrDefault();

            if (res == null) return "";

            return (res.Value??"").Trim();
        }

        public List<string> GetFormValuesById(string id)
        {
            List<string> vals = new List<string>();
            foreach (var form in Forms)
            {
                var res = form.Fields.Where(x => x.ID == id).FirstOrDefault();

                if (res == null) continue; ;

                vals.Add((res.Value ?? "").Trim());
            }

            return vals;
        }
    }
}
