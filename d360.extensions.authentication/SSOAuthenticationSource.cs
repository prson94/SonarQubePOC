using System;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Security;
using System.Linq;
using d360.core.entities;

using d360.model;
using System.Xml;
using ComponentSpace.SAML2.Protocols;
using ComponentSpace.SAML2.Assertions;
using System.Security.Cryptography.X509Certificates;
using ComponentSpace.SAML2;
using System.Web;

namespace d360.extensions.authentication
{
    public class SSOAuthenticationSource: IAuthenticationSource
    {
        CommunityContext Community;
        
        public SSOAuthenticationSource(CommunityContext community)
        {
            Community = community;
        }

        public Resource AddResource(string username, string firstName, string lastName)
        {
            var resource = new Resource {
                Email = username,
                FirstName = firstName,
                DateLastLoggedIn = DateTime.UtcNow,
                LastName = lastName,
                Password = "SSO ONLY",
                ResourceTypeID = 1,
                Status = "Active", 
                Username = username
            };
            Community.Add<Resource>(resource);

            var companyResource = new CompanyResource { 
                CompanyID = Community.CurrentCompanyID,
                IsAdministrator = false,
                ResourceID = resource.ID
            };
            Community.Add<CompanyResource>(companyResource);

            return resource;
        }

        public bool ChangePassword(int resourceID, string newPassword)
        {
            var success = false;
            return success;
        }

        public string ResetPassword(int resourceID)
        {
            string newPassword = string.Empty;
            return newPassword;
        }

        public Resource FindAuthenticatedResource(string username)
        {
            return Community.Filter<Resource>(i => i.Username == username).SingleOrDefault();
        }

        public int GetResourceIDByUsername(string username)
        {
            return Community.Filter<Resource>(i => i.Username == username).Single().ID;
        }

        public Resource ValidateResource(string username, string password)
        {
            return null;
        }

        public void Signout(HttpRequestBase request, HttpResponseBase response)
        {
            bool isRequest = false;
            string logoutReason = null;
            string partnerSP = null;

            SAMLServiceProvider.ReceiveSLO(request, out isRequest, out logoutReason, out partnerSP);

            if (isRequest)
            {
                FormsAuthentication.SignOut();                  // Logout locally.
                SAMLServiceProvider.SendSLO(response, null);    // Respond to the IdP-initiated SLO request indicating successful logout.
            }
            else
            {
                FormsAuthentication.RedirectToLoginPage();      // SP-initiated SLO has completed.
            }
        }

        private X509Certificate2 loadCertificate(string fileName, string password)
        {
            X509Certificate2 functionReturnValue = default(X509Certificate2);

            if (!System.IO.File.Exists(fileName))
            {
                throw new ArgumentException("The certificate file " + fileName + " doesn't exist.");
            }

            try
            {
                functionReturnValue = new X509Certificate2(fileName, password, X509KeyStorageFlags.MachineKeySet);
            }

            catch (Exception exception)
            {
                throw new ArgumentException("The certificate file " + fileName + " couldn't be loaded - " + exception.Message);
            }

            return functionReturnValue;
        }


        private XmlElement createAuthnRequest()
        {
            // Create the authentication request.
            AuthnRequest authnRequest = new AuthnRequest
            {
                Destination = "https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f/saml2",//Configuration.SingleSignOnServiceURL,
                Issuer = new Issuer("https://data3sixty.com/ui"),
                ForceAuthn = false,
                NameIDPolicy = new NameIDPolicy(null, null, true)
            };

            // Serialize the authentication request to XML for transmission.
            var authnRequestXml = authnRequest.ToXml();

            // Don't sign if using HTTP redirect as the generated query string is too long for most browsers.        
            //if (Configuration.SingleSignOnServiceBinding != SAMLIdentifiers.Binding.HTTPRedirect)
            //{
            // Sign the authentication request.
            //var x509Certificate = LoadCertificate(Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, "sp.pfx"), "password");
            //SAMLMessageSignature.Generate(authnRequestXml, x509Certificate.PrivateKey, x509Certificate);
            //}

            return authnRequestXml;
        }
    }
}
