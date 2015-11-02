using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using System.Web.Helpers;
using d360.web.Models;
using d360.extensions;
using System.Web.Security;
using d360.core.entities.Views;
using System.Net;
using d360.model;
using ComponentSpace.SAML2;
using ComponentSpace.SAML2.Protocols;
using System.Xml;
using ComponentSpace.SAML2.Assertions;
using System.Security.Cryptography.X509Certificates;
using ComponentSpace.SAML2.Profiles.SSOBrowser;
using ComponentSpace.SAML2.Bindings;
using System.IO;
using d360.core.enums;
using System.Reflection;
using System.Diagnostics;

namespace d360.web.Controllers
{
    [RoutePrefix("")]
    public class AuthenticationController : BaseController
    {
        #region DI

        public AuthenticationController(CommunityContext community, CompanyContext company)
            : base(community, company) 
        { }

        #endregion

        private XmlElement createAuthnRequest()
        {
            // Create the authentication request.
            AuthnRequest authnRequest = new AuthnRequest {
                AssertionConsumerServiceURL = string.Format("{0}://{1}/sso/acs", Request.Url.Scheme, Request.Url.Authority),
                Destination = Community.CurrentCompanySsoModel.IdpSsoEndpoint,// "https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f/saml2",
                //IsPassive = true,
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

            Trace.TraceInformation("createAuthnRequest => Idp Endpoint: {0}", Community.CurrentCompanySsoModel.IdpSsoEndpoint);

            return authnRequestXml;
        }

        private void verifySignature(XmlElement assertionXml)
        {
            try
            {
                if (SAMLAssertionSignature.IsSigned(assertionXml))
                {
                    Trace.TraceInformation("AssertionConsumerService => Response SAML is signed.  Verifying now...");
                    if (Community.CurrentCompanySsoModel.IdpCertificateFile != null)
                    {
                        var x509Certificate = new X509Certificate2(Community.CurrentCompanySsoModel.IdpCertificateFile);
                        if (SAMLAssertionSignature.Verify(assertionXml, x509Certificate))
                            Trace.TraceInformation("AssertionConsumerService => Response SAML is signed AND verified.");
                        else
                            throw new ApplicationException("AssertionConsumerService => Failed to Verify Signature where an IDP-supplied CER file was stored");
                    }
                    else
                    {
                        if (SAMLAssertionSignature.Verify(assertionXml))
                            Trace.TraceInformation("AssertionConsumerService => Response SAML is signed AND verified.");
                        else
                            throw new ApplicationException("AssertionConsumerService => Failed to Verify Signature where no IDP-supplied CER file was stored");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
            }        
        }

        [AllowAnonymous, Route("sso")]
        public ActionResult Login()
        {
            switch (Community.CurrentCompanySsoModel.AuthenticationType)
            { 
                case AuthenticationType.SSO:
                    var authnRequestXml = createAuthnRequest();
                    
                    string returnUrl = Request.QueryString["ReturnUrl"];

                    string relayState = null;
                    if (!string.IsNullOrWhiteSpace(returnUrl))
                        relayState = RelayStateCache.Add(new RelayState(returnUrl, null));
                    

                    Trace.TraceInformation("Login => relayState: {0}", relayState);

                    //X509Certificate2 x509Certificate = null;

                    // Send the authentication request to the identity provider over the configured binding.
                    //if (Community.CurrentCompanySsoModel.IdpCertificateFile != null) 
                    //{
                    //    x509Certificate = new X509Certificate2(Community.CurrentCompanySsoModel.IdpCertificateFile);
                    //}

                    #region Hash Choice
                    
                    //http://www.w3.org/TR/xmlsec-algorithms/

                    var hashString = "";
                    switch (Community.CurrentCompanySsoModel.HashAlgorithmType)
                    { 
                        case HashAlgorithmType.SHA1:
                            hashString = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                            break;
                        case HashAlgorithmType.SHA224:
                            hashString = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha224";
                            break;
                        case HashAlgorithmType.SHA256:
                            hashString = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                            break;
                        case HashAlgorithmType.SHA384:
                            hashString = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
                            break;
                        case HashAlgorithmType.SHA512:
                            hashString = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
                            break;
                    }

                    #endregion

                    ServiceProvider.SendAuthnRequestByHTTPRedirect(Response, Community.CurrentCompanySsoModel.IdpSsoEndpoint, authnRequestXml, relayState, null, hashString);
                                
                    return new EmptyResult();
                default:    // Login via standard forms authentication.
                    ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
                    return View();
            }
        }

        [AllowAnonymous, Route("sso/acs"), HttpPost]
        public ActionResult AssertionConsumerService()
        {
            // Extract the asserted identity from the SAML response.
            // The SAML assertion may be signed or encrypted and signed.

            SAMLResponse samlResponse = null;
            string relayState = null;

            XmlElement samlResponseXml = null;

            ServiceProvider.ReceiveSAMLResponseByHTTPPost(Request, out samlResponseXml, out relayState);
                        
            Trace.TraceInformation("AssertionConsumerService => samlResponseXml: {0}", samlResponseXml.InnerXml);

            // Deserialize the XML.
            samlResponse = new SAMLResponse(samlResponseXml);

            Trace.TraceInformation("AssertionConsumerService => IsSuccessful: {0}", samlResponse.IsSuccess() ? "Yes" : "No");

            // Check whether the SAML response indicates success or an error and process accordingly.
            if (samlResponse.IsSuccess())
            {              
                SAMLAssertion samlAssertion = null;

                Trace.TraceInformation("AssertionConsumerService => Assertion Count: {0}, Signed Assertion Count: {1}, Encrypted Assertion Count: {2}", 
                    samlResponse.GetAssertions().Count, 
                    samlResponse.GetSignedAssertions().Count,
                    samlResponse.GetEncryptedAssertions().Count);


                if (samlResponse.GetAssertions().Count > 0)
                {
                    samlAssertion = samlResponse.GetAssertions()[0];
                    verifySignature(samlAssertion.ToXml());
                }
                else if (samlResponse.GetSignedAssertions().Count > 0)
                {
                    var samlAssertionXml = samlResponse.GetSignedAssertions()[0];
                    verifySignature(samlAssertionXml);
                    samlAssertion = new SAMLAssertion(samlAssertionXml);
                }
                else if (samlResponse.GetEncryptedAssertions().Count > 0)
                {
                    // Decrypt the encrypted assertion.
                    var samlAssertionXml = samlResponse.GetAssertions()[0].ToXml();//.GetEncryptedAssertions()[0].DecryptToXml(x509Certificate.PrivateKey, null, null);
                    verifySignature(samlAssertionXml);
                    //if (SAMLAssertionSignature.IsSigned(samlAssertionXml))
                    //{
                    //    if (Community.CurrentCompanySsoModel.IdpCertificateFile != null)
                    //    {
                    //        var x509Certificate = new X509Certificate2(Community.CurrentCompanySsoModel.IdpCertificateFile);
                    //        if (!SAMLAssertionSignature.Verify(samlAssertionXml, x509Certificate))
                    //        {
                    //            Trace.TraceError("AssertionConsumerService => Failed to Verify Encrypted Assertions");
                    //            throw new ArgumentException("The SAML assertion signature failed to verify.");
                    //        }
                    //    }
                    //}
                    samlAssertion = new SAMLAssertion(samlAssertionXml);
                    //var attributes = samlAssertion.GetAttributeStatements();
                }
                else
                {
                    throw new ArgumentException("No assertions in response");
                }

                var attributes = samlAssertion.GetAttributeStatements()[0].Attributes;

                // Get the subject name identifier.
                string userName = null;
                string firstName = null;
                string lastName = null;

                foreach (SAMLAttribute a in attributes)
                {
                    Trace.TraceInformation("SAML Attribute is {0}", a.Name);

                    switch (a.Name.ToLower())
                    {
                        case "username":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                            if (a.Values.Count > 0)
                            {
                                userName = (string)a.Values[0].Data;
                            }
                            break;
                        case "first":
                        case "firstname":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":
                            if (a.Values.Count > 0)
                            {
                                firstName = (string)a.Values[0].Data;
                            }
                            break;
                        case "last":
                        case "lastname":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname":
                            if (a.Values.Count > 0)
                            {
                                lastName = (string)a.Values[0].Data;
                            }
                            break;
                    }
                }

                Trace.TraceInformation("AssertionConsumerService => Username: {0}, FirstName: {1}, LastName: {2}", userName, firstName, lastName);

                //if (samlAssertion.Subject.NameID != null) userName = samlAssertion.Subject.NameID.NameIdentifier;
                //if (string.IsNullOrEmpty(userName)) throw new ArgumentException("The SAML assertion doesn't contain a subject name.");

                Resource resource = null;

                if (!string.IsNullOrEmpty(userName))
                { 
                    userName = userName.ToLower();
                    resource = Community.Filter<Resource>(i => i.Username.ToLower() == userName).SingleOrDefault();
                    if (resource == null)
                    {
                        Trace.TraceInformation("AssertionConsumerService => Did not find resource account for Username: {0}.", userName);
                        if (Community.CurrentCompanySsoModel.AllowNewUserLogin && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                        {
                            Trace.TraceInformation("AssertionConsumerService => Now creating resource account for Username: {0}.", userName);

                            if (string.IsNullOrEmpty(firstName)) firstName = "Unknown";
                            if (string.IsNullOrEmpty(lastName)) lastName = "Unknown";

                            resource = new Resource
                            {
                                DateLastLoggedIn = DateTime.UtcNow,
                                Email = userName,
                                FirstName = firstName,
                                LastName = lastName,
                                Password = Community.createRandomPassword(),
                                ResourceTypeID = 1,
                                Status = "Active",
                                Username = userName
                            };
                            Community.Add<Resource>(resource);
                            Community.Add<CompanyResource>(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, ResourceID = resource.ID });
                            
                            Trace.TraceInformation("AssertionConsumerService => Finished creating resource account for Username: {0}.", userName);
                        }
                    }
                    else 
                    {
                        var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == resource.ID).SingleOrDefault();
                        if (companyResource == null)
                        {
                            if (Community.CurrentCompanySsoModel.AllowNewUserLogin)
                            {
                                Community.Add<CompanyResource>(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, ResourceID = resource.ID });
                            }
                            else
                            {
                                resource = null;
                            }
                        }

                        // Check b/c the company may not allow for new users to be added automatically.  If user was not already 
                        // assigned to company and auto-add not enabled, setting resource to null will prevent login.
                        if (resource != null)
                        {
                            resource.DateLastLoggedIn = DateTime.UtcNow;
                            resource.FirstName = firstName;
                            resource.LastName = lastName;
                            Community.Update<Resource>(resource);
                        }
                    }
                }

                if (resource != null)
                {
                    Trace.TraceInformation("AssertionConsumerService => Resource account exists for Username: {0}. Now authorizing with cookie.", userName);

                    // Create a login context for the asserted identity.
                    FormsAuthentication.SetAuthCookie(userName, false);

                    // Get the originally requested resource URL from the relay state, if any.
                    string redirectURL = "/#";

                    RelayState cachedRelayState = RelayStateCache.Remove(relayState);

                    if (cachedRelayState != null)
                    {
                        var sendToUrl = cachedRelayState.ResourceURL;
                        
                        if(sendToUrl.Contains("?hashPath=")) sendToUrl = Server.UrlDecode(sendToUrl.Replace("?hashPath=","#"));

                        redirectURL = sendToUrl;
                    }

                    // Redirect to the originally requested resource URL, if any, or the default page.
                    return Redirect(redirectURL);                
                }
                
                //If you go this far a problem occurred.
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                string errorMessage = null;

                if (samlResponse.Status.StatusMessage != null)
                {
                    errorMessage = samlResponse.Status.StatusMessage.Message;
                }

                Trace.TraceError("AssertionConsumerService => Unsuccessful: {0}", errorMessage);

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [AllowAnonymous, Route("sso"), HttpPost]
        public ActionResult Login(LoginModel model, string ReturnUrl)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);

            if (ModelState.IsValid)
            {
                var resource = Community.ValidateResource(model.UserName, model.Password);
                if (resource != null)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, false);
                    //if (!string.IsNullOrEmpty(ReturnUrl))
                    //{
                    //    Redirect(Server.UrlDecode(ReturnUrl));
                    //}
                    //else
                    //{
                    return Redirect("/#");
                    //}
                }
                else
                {
                    ModelState.AddModelError("Unauthorized", "The user name or password provided is incorrect.");
                    return View(model);
                }
            }

            ModelState.AddModelError("UnknownError", "An unknown error occurred.");
            return View(model);
        }

        [Route("slo")]
        public ActionResult Logout()
        {
            Session["CurrentResource"] = null;

            switch (Community.CurrentCompanySsoModel.AuthenticationType)
            { 
                case AuthenticationType.SSO:
                    // Receive the single logout request or response.
                    // If a request is received then single logout is being initiated by the identity provider.
                    // If a response is received then this is in response to single logout having been initiated by the service provider.
                    bool isRequest = false;
                    string logoutReason = null;
                    string partnerSP = null;

                    SAMLServiceProvider.ReceiveSLO(Request, out isRequest, out logoutReason, out partnerSP);

                    if (isRequest)
                    {
                        FormsAuthentication.SignOut();                  // Logout locally.
                        SAMLServiceProvider.SendSLO(Response, null);    // Respond to the IdP-initiated SLO request indicating successful logout.
                    }
                    else
                    {
                        FormsAuthentication.RedirectToLoginPage();      // SP-initiated SLO has completed.
                    }

                    return new EmptyResult();
                default:
                    FormsAuthentication.SignOut();
                    FormsAuthentication.RedirectToLoginPage();
                    return new EmptyResult();
            }
        }
    }
}
