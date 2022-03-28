using System;
using System.Web.Mvc;

using d360.extensions;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("search")]
    public class SearchController : BaseController
    {
        #region DI

        public SearchController(ICoreComponentSet set) : base(set)
        {
        }

        #endregion

        #region Json
        private JsonResult DeprecatedRedirect(string url)
        {
            Response.StatusCode = 308; // (int)System.Net.HttpStatusCode.PermanentRedirect; Enum not available
            Response.RedirectLocation = url;

            return Json(new { type = "error", title = "Permanent Redirect", message = "Permanent Redirect" }, JsonRequestBehavior.AllowGet);
        }

        private JsonNetResult DeprecatedNetRedirect(string url)
        {
            Response.StatusCode = 308; // (int)System.Net.HttpStatusCode.PermanentRedirect; Enum not available
            Response.RedirectLocation = url;

            return new JsonNetResult { };
        }

        [HttpPost, Route("Results"), NonNullableParameters]
        public JsonResult Results(QueryRequest queryRequest)
        {
            return DeprecatedRedirect("/api/v2/search/results");
        }

        [HttpGet, Route("Typeahead"), NonNullableParameters]
        [ValidateInput(false)]
        public JsonNetResult Typeahead(string q, string t, int? num)
        {
            return DeprecatedNetRedirect("/api/v2/search/typeahead");
        }

        [HttpGet, Route("Status")]
        public JsonResult Status()
        {
            return DeprecatedRedirect("/api/v2/search/status");
        }

        [HttpGet, Route("Categories")]
        public JsonResult GetCategories()
        {
            return DeprecatedRedirect("/api/v2/search/categories");
        }

        [HttpGet, Route("IndexableTypes")]
        public JsonResult GetIndexableTypes()
        {
            return DeprecatedRedirect("/api/v2/search/indexableTypes");
        }

        [HttpGet, Route("IndexableStatus")]
        public JsonResult GetIndexableStatus()
        {
            return DeprecatedRedirect("/api/v2/search/indexableStatus");
        }

        [HttpPost, Route("rebuild/{Class:int}/{assetTypeUid:Guid}")]
        public JsonResult DoRebuild(int Class, Guid assetTypeUid)
        {
            return DeprecatedRedirect("/api/v2/search/rebuild/" + Class.ToString() + "/" + assetTypeUid.ToString());
        }

        #endregion
    }
}
