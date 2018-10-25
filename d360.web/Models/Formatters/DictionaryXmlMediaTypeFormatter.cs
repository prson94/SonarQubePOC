using d360.web.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace d360.media.formatters
{
    public class DictionaryXmlMediaTypeFormatter : MediaTypeFormatter
    {
        public DictionaryXmlMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml"));
            SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml"));
        }

        public override bool CanWriteType(Type type)
        {
            return type == typeof(ArtifactModelRequestList);//(IQueryable<Dictionary<string, object>>);
        }
        public override bool CanReadType(Type type)
        {
            return type == typeof(ArtifactModelRequestList);//(IQueryable<Dictionary<string, object>>);
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var task = new TaskCompletionSource<object>();
            var ser = new XmlSerializer(type);
            task.SetResult(ser.Deserialize(readStream));
            return task.Task;
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            var task = Task.Factory.StartNew(() =>
            {                
                using (var writer = XmlTextWriter.Create(writeStream, new XmlWriterSettings() { Indent = true }))
                {
                    var dicts = (ArtifactModelRequestList)value;

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Artifacts");
                    foreach (var dict in dicts)
                    {
                        writer.WriteStartElement("Artifact");
                        foreach (var k in dict.Keys)
                        {
                            writer.WriteStartElement(k);
                            writer.WriteString(dict[k].ToString());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
                writeStream.Flush();
            });
            return task;
        }
    }
}