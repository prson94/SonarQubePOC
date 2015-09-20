using System;
using System.Collections.Generic;
using System.Linq;
using d360.core;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.Composition;

namespace d360.web.Controllers
{
    [Export("token", typeof(IController)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class TokenController : BaseController
    {
        public JsonResult Index()
        {
            //var companyID = ctx.CurrentCompanyID.ToString();
            //var privateKey = ctx.CurrentResource.APIPrivateKey;
            //var publicKey = ctx.CurrentResource.APIPublicKey;

            //var hash = new SHA256Managed();

            //long epochValue = DateTime.UtcNow.Date.Epoch();

            //string correctHash = privateKey + epochValue.ToString();
            //byte[] unhashedBytes = Encoding.ASCII.GetBytes(correctHash);
            //byte[] hashedBytes = hash.ComputeHash(unhashedBytes);
            //correctHash = Convert.ToBase64String(hashedBytes);

            string authorizationToken = "";//string.Format("{0};{1};{2}", companyID, publicKey, correctHash);
            //authorizationToken = Server.UrlEncode(authorizationToken);
            return Json(authorizationToken, JsonRequestBehavior.AllowGet);
        }
    }
}
