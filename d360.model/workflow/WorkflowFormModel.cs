using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormModel
    {
        public List<WorkflowFormFormModel> Forms { get; set; }

        public WorkflowItemStepSettingModel Settings { get; set; }

        public static WorkflowFormModel ParseXml(XElement xml)
        {
            if (xml == null)
            {
                throw new ArgumentNullException(nameof(xml), "INVALID XML SPECIFIED FOR FORM MODEL");
            }

            WorkflowFormModel model = new WorkflowFormModel
            {
                Forms = new List<WorkflowFormFormModel>()
            };

            IEnumerable<XElement> forms = xml.Elements("form");

            if (forms == null)
            {
                return model;
            }

            foreach (XElement form in forms)
            {
                model.Forms.Add(WorkflowFormFormModel.ParseXml(form));
            }

            return model;
        }

        public string GetFormValueById(string id)
        {
            WorkflowFormFormModel form = Forms.FirstOrDefault();

            if (form == null)
            {
                return "";
            }

            WorkflowFormFieldModel res = form.Fields.Where(x => x.ID == id).FirstOrDefault();

            if (res == null)
            {
                return "";
            }

            return (res.Value ?? "").Trim();
        }

        public List<string> GetFormValuesById(string id)
        {
            List<string> vals = new List<string>();
            foreach (WorkflowFormFormModel form in Forms)
            {
                WorkflowFormFieldModel res = form.Fields.Where(x => x.ID == id).FirstOrDefault();

                if (res != null)
                {
                    vals.Add((res.Value ?? "").Trim());
                };                
            }

            return vals;
        }
    }
}
