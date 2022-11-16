using System.IO;
using System.Web;
using System.Web.SessionState;
using d360.web.Utilities;

namespace WebApp
{
	public class ChunkLocaleJSHandler : IHttpHandler, IRequiresSessionState
	{
		public void ProcessRequest(HttpContext context)
		{
			string fileToServe = context.Request.Path;
			string userSettingLocale = null;

			var owinContext = context.GetOwinContext();
			if (owinContext != null)
			{
				userSettingLocale = owinContext.Get<string>("ApplicationLanguageSetting");
			}

			if (fileToServe.Contains("chunks"))
			{
				var localeCode = InternationalizationUtilities.GetUserLocaleCode(userSettingLocale);
				fileToServe = fileToServe.Replace("chunks", localeCode);
			}

			FileInfo jsFile = new FileInfo(context.Server.MapPath(fileToServe));
			context.Response.ClearContent();
			context.Response.ContentType = "text/javascript";
			context.Response.AddHeader("Content-Length", jsFile.Length.ToString());
			context.Response.TransmitFile(jsFile.FullName);
			context.Response.Flush();
			context.Response.End();
		}

		public bool IsReusable => false;
	}
}
