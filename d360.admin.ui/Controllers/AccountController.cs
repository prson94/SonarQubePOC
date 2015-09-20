using ComponentSpace.SAML2;
using ComponentSpace.SAML2.Assertions;
using ComponentSpace.SAML2.Bindings;
using ComponentSpace.SAML2.Profiles.SSOBrowser;
using ComponentSpace.SAML2.Protocols;
using d360.core.entities;
using d360.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;

namespace d360.admin.ui.Controllers
{
    public class AccountController : Controller
    {
        string ssoEndpoint = "https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f/saml2";

        #region DI

        CommunityContext Community;

        public AccountController(CommunityContext community)
        { 
            Community = community;
        }

        #endregion

        private XmlElement createAuthnRequest()
        {
            // Create the authentication request.
            AuthnRequest authnRequest = new AuthnRequest {
                AssertionConsumerServiceURL = string.Format("{0}://{1}/sso/acs", Request.Url.Scheme, Request.Url.Authority),
                Destination = Community.CurrentCompanySsoModel.IdpSsoEndpoint,// "https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f/saml2",
                //IsPassive = true,
                Issuer = new Issuer("https://data3sixty.com/d360.admin.ui"),
                ForceAuthn = false,
                NameIDPolicy = new NameIDPolicy(null, null, true)
            };
            
            // Serialize the authentication request to XML for transmission.
            var authnRequestXml = authnRequest.ToXml();

            return authnRequestXml;
        }

        private void verifySignature(XmlElement assertionXml)
        {
            try
            {
                if (SAMLAssertionSignature.IsSigned(assertionXml))
                {
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
            var authnRequestXml = createAuthnRequest();
            //string spResourceURL = new Uri(Request.Url, FormsAuthentication.GetRedirectUrl("", false)).ToString();
            string relayState = string.Format("{0}://{1}/sso/acs", Request.Url.Scheme, Request.Url.Authority);//RelayStateCache.Add(new RelayState(spResourceURL, null));

            Trace.TraceInformation("Login => relayState: {0}", relayState);

            var hashString = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

            ServiceProvider.SendAuthnRequestByHTTPRedirect(Response, ssoEndpoint, authnRequestXml, relayState, null, hashString);
            
            return new EmptyResult();
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

                Resource resource = null;

                if (!string.IsNullOrEmpty(userName))
                { 
                    userName = userName.ToLower();
                    resource = Community.Filter<Resource>(i => i.Username.ToLower() == userName).SingleOrDefault();
                    if (resource == null)
                    {
                        Trace.TraceInformation("AssertionConsumerService => Did not find resource account for Username: {0}.", userName);
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
                        redirectURL = cachedRelayState.ResourceURL;
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

        [Route("slo")]
        public ActionResult Logout()
        {
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
        }
    }
}