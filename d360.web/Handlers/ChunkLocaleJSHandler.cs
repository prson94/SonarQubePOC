using d360.web.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
namespace WebApp
{
    public class ChunkLocaleJSHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string fileToServe = context.Request.Path;

            if (fileToServe.Contains("chunks"))
            {
                fileToServe = fileToServe.Replace("chunks", "fr");
            }
            FileInfo jsFile = new FileInfo(context.Server.MapPath(fileToServe));
            context.Response.ClearContent();
            context.Response.ContentType = "text/javascript";
            context.Response.AddHeader("Content-Length", jsFile.Length.ToString());
            context.Response.TransmitFile(jsFile.FullName);
            context.Response.Flush();
            context.Response.End();

        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}