using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;



namespace d360.model.workflow
{

    public class WorkflowHttpResponseSettingsModel
    {
        private static string INPUTSTEP_ID = "InputStepId";
        private static string INPUTSTEP_NAME = "InputStepName";
        private static string OUTPUTS_VALUE = "Outputs";

        public string InputStepId { get; set; }
        public string InputStepName { get; set; }
        public List<WorkflowHttpResponseOutput> Outputs { get; set; }


        public static WorkflowHttpResponseSettingsModel ParseXml(XElement xml)
        {
            var model = new WorkflowHttpResponseSettingsModel();
            var outputs = new List<WorkflowHttpResponseOutput>();

            if (xml.Element(INPUTSTEP_ID) != null)
            {
                model.InputStepId = xml.Element(INPUTSTEP_ID).Value;
            }

            if (xml.Element(INPUTSTEP_NAME) != null)
            {
                model.InputStepName = xml.Element(INPUTSTEP_NAME).Value;
            }

            if (xml.Element(OUTPUTS_VALUE) != null)
            {
                foreach (var field in xml.Elements(OUTPUTS_VALUE))
                {
                    outputs.Add(WorkflowHttpResponseOutput.ParseXml(field));
                }
            }

            model.Outputs = outputs;

            return model;
        }
    }

    public class WorkflowHttpResponseOutput
    {
        public string Id { get; set; }
        public string StepId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = "text";
        public string Format { get; set; } = "json";
        public string Path { get; set; }

        public static WorkflowHttpResponseOutput ParseXml(XElement xml)
        {
            var model = new WorkflowHttpResponseOutput();

            if (xml.Element("Id") != null)
            {
                model.Id = xml.Element("Id").Value;
            }
            if (xml.Element("StepId") != null)
            {
                model.StepId = xml.Element("StepId").Value;
            }
            if (xml.Element("Name") != null)
            {
                model.Name = xml.Element("Name").Value;
            }
            if (xml.Element("Type") != null)
            {
                model.Type = xml.Element("Type").Value;
            }
            if (xml.Element("Format") != null)
            {
                model.Format = xml.Element("Format").Value;
            }
            if (xml.Element("Path") != null)
            {
                model.Path = xml.Element("Path").Value;
            }

            return model;
        }
    }
}

