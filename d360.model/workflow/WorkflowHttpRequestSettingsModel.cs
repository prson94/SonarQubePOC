using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowHttpRequestSettingsModel
    {
        private static readonly string METHOD_VALUE = "Method";
        private static readonly string URL_VALUE = "Url";
        private static readonly string HEADERS_VALUE = "Headers";
        private static readonly string TIMEOUT_VALUE = "Timeout";
        private static readonly string BODY_VALUE = "Body";
        private static readonly string STATUS_CODE_VALUE = "StatusCode";
        private static readonly string LOOKUPFIELDSPASSEDBYVALUE_VALUE = "lookupFieldsPassedByValue";

        public string Method { get; set; }

        public string Url { get; set; }
        
        public int Timeout { get; set; }
        
        public int StatusCode { get; set; }
        
        public string Body { get; set; }
        
        public List<WorkflowHttpRequestHeader> Headers { get; set; }
        
        public bool LookupFieldsPassedByValue { get; set; }
        
        public Uri FormattedUrl { get; set; }

        public static WorkflowHttpRequestSettingsModel ParseXml(XElement xml)
        {
            WorkflowHttpRequestSettingsModel model = new WorkflowHttpRequestSettingsModel();
            List<WorkflowHttpRequestHeader> headers = new List<WorkflowHttpRequestHeader>();
            int timeout = 90;
            int statusCode = 0;
            bool lookupFieldsPassedByValue = false;

            if (xml.Element(METHOD_VALUE) != null)
            {
                model.Method = xml.Element(METHOD_VALUE).Value;
            }

            if (xml.Element(URL_VALUE) != null)
            {
                model.Url = xml.Element(URL_VALUE).Value;
            }

            if (xml.Element(BODY_VALUE) != null)
            {
                model.Body = xml.Element(BODY_VALUE).Value;
            }

            if (xml.Element(TIMEOUT_VALUE) != null)
            {
                int.TryParse(xml.Element(TIMEOUT_VALUE).Value, out timeout);
            }

            model.Timeout = timeout;

            if (xml.Element(LOOKUPFIELDSPASSEDBYVALUE_VALUE) != null)
            {
                bool.TryParse(xml.Element(LOOKUPFIELDSPASSEDBYVALUE_VALUE).Value, out lookupFieldsPassedByValue);
            }

            model.LookupFieldsPassedByValue = lookupFieldsPassedByValue;

            if (xml.Element(STATUS_CODE_VALUE) != null)
            {
                int.TryParse(xml.Element(STATUS_CODE_VALUE).Value, out statusCode);
            }

            model.StatusCode = statusCode;

            if (xml.Element(HEADERS_VALUE) != null)
            {
                foreach (XElement field in xml.Elements(HEADERS_VALUE))
                {
                    headers.Add(WorkflowHttpRequestHeader.ParseXml(field));
                }
            }

            model.Headers = headers;

            return model;
        }
    }

    public class WorkflowHttpRequestHeader
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public static WorkflowHttpRequestHeader ParseXml(XElement xml)
        {
            WorkflowHttpRequestHeader model = new WorkflowHttpRequestHeader();

            if (xml.Element("key") != null)
            {
                model.Key = xml.Element("key").Value;
            }

            if (xml.Element("value") != null)
            {
                model.Value = xml.Element("value").Value;
            }

            return model;
        }
    }
}
