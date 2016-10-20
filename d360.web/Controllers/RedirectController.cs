using System.Web.Mvc;

namespace d360.web.Controllers
{
    [HandleError(View = "Error"), Authorize, RoutePrefix("redirect")]
    public class RedirectController : Controller
    {
        [Authorize, Route("file")]
        public RedirectResult File(string path)
        {
            return new RedirectResult(@"file://" + Server.UrlDecode(path).Replace(@"file://", "").Replace("\\", "/"));
        }
    }
}
