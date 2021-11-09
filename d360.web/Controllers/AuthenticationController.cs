using ComponentSpace.SAML2.Assertions;
using ComponentSpace.SAML2.Profiles.SingleLogout;
using ComponentSpace.SAML2.Profiles.SSOBrowser;
using ComponentSpace.SAML2.Protocols;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.extensions.azuregraph;
using d360.extensions.mail;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Extensions;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using IdentityModel.Client;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using Resources;
using d360.extensions;

namespace d360.web.Controllers
{
    [RoutePrefix(""), ValidateContracts(Ignore = true)]
    public class AuthenticationController : BaseController
    {
        const string APP_ID = "https://data3sixty.com/ui";
        const string SessionIndexCookieName = "SessionIndex";

        #region DI

        TelemetryClient Telemetry;

        public AuthenticationController(ICommunityContext community, ICompanyContext company, IMailProvider mail, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            Mail = mail;
            Telemetry = new TelemetryClient();
            Telemetry.Context.InstrumentationKey = ConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            Telemetry.Context.GlobalProperties["CompanyID"] = company.CurrentCompanyID.ToString();
        }

        #endregion

        private XmlElement createAuthnRequest()
        {
            // Create the authentication request.
            AuthnRequest authnRequest = new AuthnRequest
            {
                AssertionConsumerServiceURL = string.Format("{0}://{1}/sso/acs", Request.Url.Scheme, Request.Url.Authority),
                Destination = Community.CurrentCompanySsoModel.IdpSsoEndpoint,// "https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f/saml2",                
                Issuer = new Issuer(APP_ID),
                ForceAuthn = false,
                NameIDPolicy = new NameIDPolicy(null, null, true)
            };

            // Serialize the authentication request to XML for transmission.
            var authnRequestXml = authnRequest.ToXml();

            Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = $"createAuthnRequest => Idp Endpoint: {Community.CurrentCompanySsoModel.IdpSsoEndpoint}" });

            return authnRequestXml;
        }

        private void verifySignature(XmlElement assertionXml)
        {
            try
            {
                if (SAMLAssertionSignature.IsSigned(assertionXml))
                {
                    Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = "AssertionConsumerService => Response SAML is signed.  Verifying now..." });

                    if (Community.CurrentCompanySsoModel.IdpCertificateFile != null)
                    {
                        var x509Certificate = new X509Certificate2(Community.CurrentCompanySsoModel.IdpCertificateFile);
                        if (SAMLAssertionSignature.Verify(assertionXml, x509Certificate))
                        {
                            Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = "AssertionConsumerService => Response SAML is signed AND verified." }); //Trace.TraceInformation("AssertionConsumerService => Response SAML is signed AND verified.");
                        }
                        else
                        {
                            throw new ArgumentNullException(OthersMessages.FailedToVerifySignature);
                        }
                    }
                    else
                    {
                        if (SAMLAssertionSignature.Verify(assertionXml))
                        {
                            Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = "AssertionConsumerService => Response SAML is signed AND verified." });
                        }
                        else
                        {
                            throw new ArgumentNullException(OthersMessages.FailedToVerifySignatureNoIDP);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Error, Message = ex.Message + ((ex.InnerException != null) ? ex.InnerException.Message : "") });

            }

        }

        private ActionResult parseUserInfoAndLogin(
            string userName, string firstName, string lastName, 
            List<string> groups = null, Dictionary<string, string> customClaims = null,
            string relayState = null,
            System.Action customAction = null)
        {
            Resource resource = null;

            if (!string.IsNullOrEmpty(userName))
            {
                userName = userName.ToLower();
                resource = Community.Filter<Resource>(i => i.Username.ToLower() == userName).SingleOrDefault();

                // If user is assigned to any groups in SAML claims, then check to see if any of those groups should be assigned as admin. If so, assign the user as admin.

                bool isCompanyAdministrator = false;
                if (groups?.Any() == true)
                {
                    isCompanyAdministrator = Community.CompanyDomainGroups.Any(g =>
                        g.CompanyID == Community.CurrentCompanyID &&
                        g.DomainSettingID == Community.CurrentDomainSettingID &&
                        groups.Contains(g.GroupName) &&
                        g.IsAdministrator);
                }

                if (resource == null)
                {
                    Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Did not find resource account for Username: {userName}.", SeverityLevel = SeverityLevel.Warning });

                    if (Community.CurrentCompanySsoModel.AllowNewUserLogin && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Now creating resource account for Username: {userName}.", SeverityLevel = SeverityLevel.Information });

                        if (string.IsNullOrEmpty(firstName))
                        {
                            firstName = "Unknown";
                        }
                        if (string.IsNullOrEmpty(lastName))
                        {
                            lastName = "Unknown";
                        }

                        resource = new Resource
                        {
                            Email = userName,
                            FirstName = firstName,
                            LastName = lastName,
                            Password = PasswordHelper.CreateRandomPassword(),
                            Username = userName
                        };
                        Community.Add(resource);

                        var companyResource = new CompanyResource
                        {
                            CompanyID = Community.CurrentCompanyID,
                            IsAdministrator = isCompanyAdministrator,
                            ResourceID = resource.ID,
                            LastLoggedInOn = DateTime.UtcNow,
                            State = CompanyResourceState.Active
                        };
                        Community.Add(companyResource);

                        if (!Company.Any<GlobalReportingResource>(gr => gr.ResourceID == resource.ID))
                        {
                            Company.Add(new GlobalReportingResource
                            {
                                LastLoggedInOn = companyResource.LastLoggedInOn,
                                Email = resource.Email,
                                FirstName = resource.FirstName,
                                LastName = resource.LastName,
                                IsAdministrator = false,
                                ResourceID = resource.ID,
                                Uid = resource.Uid,
                                State = companyResource.State,
                                CreatedOn = DateTime.UtcNow
                            });
                        }

                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Finished creating resource account for Username: {userName}.", SeverityLevel = SeverityLevel.Verbose });
                    }
                }
                else
                {
                    var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == resource.ID).SingleOrDefault();
                    if (companyResource == null)
                    {
                        if (Community.CurrentCompanySsoModel.AllowNewUserLogin)
                        {
                            companyResource = new CompanyResource
                            {
                                CompanyID = Community.CurrentCompanyID,
                                IsAdministrator = isCompanyAdministrator,
                                ResourceID = resource.ID,
                                LastLoggedInOn = DateTime.UtcNow,
                                State = CompanyResourceState.Active
                            };
                            Community.Add(companyResource);
                            if (!Company.Any<GlobalReportingResource>(gr => gr.ResourceID == resource.ID))
                            {
                                Company.Add(new GlobalReportingResource
                                {
                                    LastLoggedInOn = companyResource.LastLoggedInOn,
                                    Email = resource.Email,
                                    FirstName = resource.FirstName,
                                    LastName = resource.LastName,
                                    IsAdministrator = false,
                                    ResourceID = resource.ID,
                                    Uid = resource.Uid,
                                    State = companyResource.State,
                                    CreatedOn = DateTime.UtcNow
                                });
                            }
                        }
                        else
                        {
                            resource = null;
                        }
                    }
                    else
                    {
                        if (companyResource.State == CompanyResourceState.Active)
                        {
                            // We will not support downgrading users from admin to non-admin, ONLY upgrading (GOV-13515).
                            if (isCompanyAdministrator)
                            {
                                companyResource.IsAdministrator = isCompanyAdministrator;
                            }
                            companyResource.LastLoggedInOn = DateTime.UtcNow;
                            Community.Update(companyResource);
                        }
                        else
                        {
                            // The company resource account is not active, so ensure that user is NOT able to log in.
                            resource = null;
                        }
                    }

                    // Check b/c the company may not allow for new users to be added automatically.  If user was not already 
                    // assigned to company and auto-add not enabled, setting resource to null will prevent login.
                    if (resource != null)
                    {
                        bool userCorePropertiesChanged = false;

                        if (!string.IsNullOrEmpty(firstName))
                        {
                            if (resource.FirstName != firstName)
                            {
                                userCorePropertiesChanged = true;
                                resource.FirstName = firstName;
                            }
                        }

                        if (!string.IsNullOrEmpty(lastName))
                        {
                            if (resource.LastName != lastName)
                            {
                                userCorePropertiesChanged = true;
                                resource.LastName = lastName;
                            }
                        }

                        if (userCorePropertiesChanged)
                        {
                            Community.Update(resource);
                        }
                    }
                    else
                    {
                        return Redirect("/noaccess");
                    }
                }
            }

            if (resource != null)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Resource account exists for Username: {userName}. Now authorizing with cookie.", SeverityLevel = SeverityLevel.Verbose });

                if (resource.ID > 0)
                {
                    var sessionLengthMinutes = SettingsRepository.GetSettingValue<double>(Setting.SessionTimeout);

                    // Create a login context for the asserted identity.

                    #region Process Group claims
                    if (groups?.Any() == true)
                    {
                        var governHasGroups = Company.Groups.Any(g => g.IsActiveDirectoryGroup);
                        if (governHasGroups)
                        {
                            if (Company.Connection.State != ConnectionState.Open)
                            {
                                Company.Connection.Open();
                            }
                            using (var trans = Company.Connection.BeginTransaction())
                            {
                                try
                                {
                                    SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(Company.Connection, SqlBulkCopyOptions.Default, trans);
                                    var dt = new System.Data.DataTable();
                                    dt.Columns.Add("Name", typeof(string));

                                    groups.ForEach(g =>
                                    {
                                        var row = dt.NewRow();
                                        row["Name"] = g.Trim();
                                        dt.Rows.Add(row);
                                    });


                                    sqlBulkCopy.ColumnMappings.Add("Name", "Name");
                                    sqlBulkCopy.DestinationTableName = "#ADGroups";
                                    sqlBulkCopy.BulkCopyTimeout = 60;

                                    Company.Connection.Execute(
                                        @"drop table if exists #ADGroups;
                                            create table #ADGroups ([Name] nvarchar(max), GroupID int, HasResourceGroup bit);"
                                    , transaction: trans);

                                    sqlBulkCopy.WriteToServer(dt);

                                    Company.Connection.Execute(
                                        @"update A
                                            set A.GroupID = G.ID,
                                            HasResourceGroup = case when RG.ResourceID is not null then 1 else null end
                                            from    #ADGroups A
                                                    inner join [Group] G on G.IsActiveDirectoryGroup = 1 and G.[Name] = A.[name]
                                                    left join ResourceGroup RG on RG.GroupID = G.ID and RG.ResourceID = @resourceId

                                            insert into ResourceGroup (ResourceID, GroupID)
                                            select  @resourceId, GroupID
                                            from    #ADGroups 
                                            where   GroupID is not null and coalesce(HasResourceGroup, 0) = 0

                                            delete  R
                                            from    ResourceGroup R
                                                    inner join [Group] G on G.ID = R.GroupID and G.IsActiveDirectoryGroup = 1
                                            where   R.ResourceID = @resourceId and not exists (select 1 from #ADGroups where GroupID = R.GroupID)"
                                    , new { resourceID = resource.ID }
                                    , transaction: trans);

                                    trans.Commit();

                                }
                                catch (Exception e)
                                {
                                    try
                                    {
                                        if (trans != null)
                                        {
                                            trans.Rollback();
                                        }
                                    }
                                    catch 
                                    {
                                        // Do nothing.
                                    }

                                    var properties = new Dictionary<string, string>
                                        {
                                            {"ResourceID",resource.ID.ToString() }
                                        };
                                    Telemetry.TrackException(e, properties);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Process custom claims

                    try
                    {
                        var resourceTypeFields = Company.Filter<FieldType>(i => i.Object == "ResourceType").ToList();
                        var resourceFields = Company.Filter<Field>(i => i.ObjectType == "Resource" && i.ObjectID == resource.ID).ToList();
                        var shouldSaveFields = false;

                        foreach (var f in resourceTypeFields.Where(i => customClaims.Keys.Contains(i.Name.ToLower())))
                        {
                            var claimName = f.Name.Trim().ToLower();

                            var rf = resourceFields.FirstOrDefault(i => i.FieldTypeID == f.ID);
                            if (rf != null)
                            {
                                if (rf.Value != customClaims[claimName])
                                {
                                    rf.Value = customClaims[claimName];
                                    shouldSaveFields = true;
                                }
                            }
                            else
                            {
                                rf = new Field { FieldTypeID = f.ID, ObjectType = "Resource", ObjectID = resource.ID, Value = customClaims[claimName] };
                                Company.Fields.Add(rf);
                                shouldSaveFields = true;
                            }
                        }

                        if (shouldSaveFields)
                        {
                            Company.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Error processing custom claims for: {userName}. Error is: {ex.Message}.", SeverityLevel = SeverityLevel.Error });
                    }

                    #endregion

                    var ticket = new FormsAuthenticationTicket(
                        1,
                        userName,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(sessionLengthMinutes),
                        false,
                        $"userName, {Request.UserAgent}",
                        FormsAuthentication.FormsCookiePath
                    );
                    var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                    {
                        HttpOnly = true,
                        Secure = FormsAuthentication.RequireSSL,
                        Path = FormsAuthentication.FormsCookiePath,
                        Domain = FormsAuthentication.CookieDomain
                    };

                    Response.AppendCookie(cookie);

                    if (customAction != null)
                    {
                        customAction();
                    }

                    // Get the originally requested resource URL from the relay state, if any.
                    string redirectURL = "/#";
                    try
                    {
                        if (!string.IsNullOrEmpty(relayState))
                        {
                            // check for absolute url to prevent open redirect security vulnerability https://cwe.mitre.org/data/definitions/601.html 
                            // if relaystate contains // which is an absolute url examples:
                            // https://www.cnn.com
                            // http://www.foxnews.com
                            // //stackoverflow.com
                            // www.cnn.com, /artifact, /artifact/1 will be treated as relative urls and will just get stuck on end of current path

                            if (!relayState.Contains("//"))
                            {
                                redirectURL = relayState;
                            }
                        }
                    }
                    catch (Exception e)
                    {

                        var properties = new Dictionary<string, string>
                            {
                                {"ResourceID",resource.ID.ToString() }
                            };
                        Telemetry.TrackException(e, properties);

                        redirectURL = "/#";
                    }

                    // Redirect to the originally requested resource URL, if any, or the default page.                        
                    return Redirect(redirectURL);
                }
                else
                {
                    Telemetry.TrackTrace(
                        new TraceTelemetry
                        {
                            Message = $"AssertionConsumerService => Referencing resource: {resource.ID}. Should not authorize with the system account.  The username is: {userName}",
                            SeverityLevel = SeverityLevel.Error
                        });
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest); //If you made this far, then error occurred.
        }

        [AllowAnonymous, Route("sso")]
        public ActionResult Login()
        {
            if (Request.Browser.Browser.ToLower() == "internetexplorer")
            {
                return RedirectToAction("unsupported", "home");
            }

            if (!Community.CurrentCompanySsoModel.IsCompanyActive)
            {
                return InactiveCompany();
            }

            string returnUrl = Request.QueryString["ReturnUrl"];

            Uri testUri;
            Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out testUri);

            if (testUri.IsAbsoluteUri)
                returnUrl = "/home";

            switch (Community.CurrentCompanySsoModel.AuthenticationType)
            {
                case AuthenticationType.Saml:
                    #region
                    var authnRequestXml = createAuthnRequest();

                    Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = $"Login => relayState: {returnUrl}" });

                    if (Community.CurrentCompanySsoModel.SignInitialSSORequest)
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = $"Login => signing initial authentication request" });

                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("d360.web.d3s-signing.pfx"))
                        {
                            var bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);
                            X509Certificate2 x509Certificate = new X509Certificate2(bytes, "D3S");

                            ServiceProvider.SendAuthnRequestByHTTPRedirect(Response, Community.CurrentCompanySsoModel.IdpSsoEndpoint, authnRequestXml, returnUrl, x509Certificate != null ? x509Certificate.PrivateKey : null, "http://www.w3.org/2000/09/xmldsig#rsa-sha1");
                        }
                    }
                    else
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { SeverityLevel = SeverityLevel.Verbose, Message = $"Login => not signing initial authentication request" });

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

                        ServiceProvider.SendAuthnRequestByHTTPRedirect(Response, Community.CurrentCompanySsoModel.IdpSsoEndpoint, authnRequestXml, returnUrl, null, hashString);
                    }

                    return new EmptyResult();
                    #endregion
                case AuthenticationType.OpenId:
                    var authenticationSettings = Community.CurrentCompanySsoModel.StructuredAuthenticationSettings;

                    if (string.IsNullOrEmpty(authenticationSettings.baseUri) || string.IsNullOrEmpty(authenticationSettings.clientId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ApiMessages.MissingConfigInfo);
                    }
                    var state = Community.GenerateOpenIdRequestValue();
                    var nonce = Community.GenerateOpenIdRequestValue();
                    var callbackUri = $"{Request.Url.Scheme}://{Request.Url.Authority}/sso/openid";

                    Community.SetOpenIdRequest(new OpenIdRequest { Nonce = nonce, RedirectUrl = returnUrl, State = state });

                    var ru = new RequestUrl($"{authenticationSettings.baseUri}/authorize");
                    var extras = new Parameters();
                    if (authenticationSettings.extraParameters.Properties().Count() > 0)
                    {
                        foreach (var p in authenticationSettings.extraParameters.Properties())
                        {
                            extras.Add(p.Name, p.Value.ToString());
                        }
                    }
                    var url = ru.CreateAuthorizeUrl(
                        clientId: authenticationSettings.clientId, 
                        responseType: "code", 
                        scope: "openid profile email infogix", //infogix
                        callbackUri, 
                        state, 
                        nonce, 
                        responseMode: "form_post",
                        extra: extras
                        );
                    return new RedirectResult(url);
                default:    // Login via standard forms authentication.
                    ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
                    ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
                    return View();
            }
        }

        [AllowAnonymous, Route("sso/acs"), HttpPost]
        public ActionResult ParseSamlResponse()
        {
            // Extract the asserted identity from the SAML response.
            // The SAML assertion may be signed or encrypted and signed.
            var telemetry = new TelemetryClient();

            SAMLResponse samlResponse = null;
            string relayState = null;

            XmlElement samlResponseXml = null;

            ServiceProvider.ReceiveSAMLResponseByHTTPPost(Request, out samlResponseXml, out relayState);

            Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => samlResponseXml: {samlResponseXml.InnerXml}", SeverityLevel = SeverityLevel.Information });

            // Deserialize the XML.
            samlResponse = new SAMLResponse(samlResponseXml);

            Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => IsSuccessful: {(samlResponse.IsSuccess() ? "Yes" : "No")}", SeverityLevel = SeverityLevel.Information });

            // Check whether the SAML response indicates success or an error and process accordingly.
            if (samlResponse.IsSuccess())
            {
                SAMLAssertion samlAssertion = null;

                Telemetry.TrackTrace(
                    new TraceTelemetry
                    {
                        Message = $"AssertionConsumerService => Assertion Count: {samlResponse.GetAssertions().Count}, Signed Assertion Count: {samlResponse.GetSignedAssertions().Count}, Encrypted Assertion Count: {samlResponse.GetEncryptedAssertions().Count}",
                        SeverityLevel = SeverityLevel.Information
                    });

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
                    var samlAssertionXml = samlResponse.GetAssertions()[0].ToXml();
                    verifySignature(samlAssertionXml);

                    samlAssertion = new SAMLAssertion(samlAssertionXml);

                }
                else
                {
                    throw new ArgumentException(OthersMessages.NoAssertionsInResponse);
                }

                var attributes = samlAssertion.GetAttributeStatements()[0].Attributes;

                // Get the subject name identifier.
                string userName = null;
                string firstName = null;
                string lastName = null;
                List<string> groups = null;

                var customClaims = new Dictionary<string, string>();

                var submittedAttributes = "";
                foreach (SAMLAttribute a in attributes)
                {
                    var attName = a.Name.ToLower().Trim();
                    var attValue = "";
                    if (a.Values.Count > 0)
                    {
                        attValue = (string)a.Values[0].Data;
                    }
                    submittedAttributes += $"{attName}: {attValue}; ";

                    switch (attName)
                    {
                        case "username":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                            userName = attValue;
                            break;
                        case "first":
                        case "firstname":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":
                            firstName = attValue;
                            break;
                        case "last":
                        case "lastname":
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname":
                            lastName = attValue;
                            break;
                        case "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups":
                            groups = a.Values?.Select(v => v.Data.ToString())?.ToList();
                            break;
                        default:
                            customClaims.Add(attName, attValue);
                            break;
                    }
                }

                Telemetry.TrackTrace(new TraceTelemetry { Message = $"SAML Attributes are: {submittedAttributes}", SeverityLevel = SeverityLevel.Verbose });
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Username: {userName}, FirstName: {firstName}, LastName: {lastName}", SeverityLevel = SeverityLevel.Information });

                System.Action addSamlAssertionToCookie = () => {
                    if (!string.IsNullOrEmpty(samlAssertion.ID))
                    {
                        var samlCookie = new HttpCookie(SessionIndexCookieName, samlAssertion.ID)
                        {
                            HttpOnly = true,
                            Secure = FormsAuthentication.RequireSSL
                        };

                        Response.AppendCookie(samlCookie);
                    }
                };

                return parseUserInfoAndLogin(userName, firstName, lastName, groups, customClaims, relayState, addSamlAssertionToCookie);
            }
            else
            {
                string errorMessage = null;

                if (samlResponse.Status.StatusMessage != null)
                {
                    errorMessage = samlResponse.Status.StatusMessage.Message;
                }
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AssertionConsumerService => Unsuccessful: {errorMessage}", SeverityLevel = SeverityLevel.Error });

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [AllowAnonymous, Route("sso/openid"), HttpPost]
        public async Task<ActionResult> ParseOpenIdResponse()
        {
            // From IdP.
            var code = Request.Form["code"];
            var state = Request.Form["state"];

            var authenticationSettings = Community.CurrentCompanySsoModel.StructuredAuthenticationSettings;

            if (string.IsNullOrEmpty(authenticationSettings.baseUri) || string.IsNullOrEmpty(authenticationSettings.clientId) || string.IsNullOrEmpty(authenticationSettings.clientSecret) || string.IsNullOrEmpty(authenticationSettings.audience))
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ApiMessages.MissingConfigInfo);
            }

            var baseUri = authenticationSettings.baseUri;
            var openIdRequest = Community.GetOpenIdRequest(state);
            if (openIdRequest == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ApiMessages.FailedAuthentication);
            }

            var client = new HttpClient();
            string redirectUri = $"{Request.Url.Scheme}://{Request.Url.Authority}/sso/openid";
            var response = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = $"{baseUri}/token",

                ClientId = authenticationSettings.clientId,
                ClientSecret = authenticationSettings.clientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                Code = code,
                Method = HttpMethod.Post,
                RedirectUri = redirectUri
            });
            if (response.IsError)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, response.HttpErrorReason);
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(response.IdentityToken);
            
            var accessToken = handler.ReadJwtToken(response.AccessToken);

            var incomingNonce = token.Claims.SingleOrDefault(c => c.Type == "nonce").Value.ToString();
            if (openIdRequest.Nonce != incomingNonce)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ApiMessages.FailedAuthenticationNonces);
            }

            #region Claims processing

            var combinedClaims = new List<System.Security.Claims.Claim>();
            var customClaims = new Dictionary<string, string>();
            string userName = null;
            string firstName = null;
            string lastName = null;
            List<string> groups = new List<string>();

            combinedClaims.AddRange(token.Claims); // ID token claims.
            combinedClaims.AddRange(accessToken.Claims.Except(token.Claims)); // Access token claims.

            try
            {
                foreach (var prop in combinedClaims)
                {
                    switch (prop.Type.ToLower())
                    {
                        case "amr":
                        case "aud":
                        case "at_hash":
                        case "auth_time":
                        case "cid":
                        case "exp":
                        case "iat":
                        case "idp":
                        case "iss":
                        case "jti":
                        case "name":
                        case "nonce":
                        case "preferred_username":
                        case "scp":
                        case "ver":
                        case "uid":
                            break;
                        case "sub":
                        case "email":
                            userName = prop.Value.ToString();
                            break;
                        case "givenname":
                        case "given_name":
                        case "firstname":
                            firstName = prop.Value.ToString();
                            break;
                        case "family_name":
                        case "lastname":
                        case "surname":
                            lastName = prop.Value.ToString();
                            break;
                        case "infogixgroup":
                        case "infogixgroups":
                        case "group":
                        case "groups":
                        case "securitygroups":
                        case "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups":
                            if (groups == null)
                            {
                                groups = new List<string>();
                            }
                            groups.Add(prop.Value.ToString());
                            break;
                        default:
                            customClaims.Add(prop.Type, prop.Value.ToString());
                            break;
                    }
                }
            }
            catch
            {
                //nothing
            }

            #endregion

            string redirectUrl = openIdRequest.RedirectUrl;
            try
            {
                Community.RemoveOpenIdRequest(openIdRequest);
            }
            catch (Exception ex)
            {
                this.SendException(ex, new Dictionary<string, string> { { "State", openIdRequest.State } });
            }
            

            try
            {
                var discoveryUri = string.IsNullOrEmpty(authenticationSettings.discoveryUri) ? baseUri : authenticationSettings.discoveryUri;
                var disco = new DiscoveryCache(discoveryUri, new DiscoveryPolicy { RequireHttps = true, ValidateEndpoints = false });
                var discoDoc = disco.GetAsync().Result;


                var keySet = await client.GetJsonWebKeySetAsync($"{baseUri}/keys");

                var user = response.IdentityToken.ValidateJwtIdentityToken(authenticationSettings.nameClaimType,
                    authenticationSettings.audience, false, 
                    discoDoc.Issuer, (discoDoc.Issuer!=null), 
                    keySet.KeySet.Keys, true, true, true, false);

                System.Action addOpenIdTokenToContext = () => {
                    var properties = new Microsoft.Owin.Security.AuthenticationProperties();
                    var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);

                    Response.AppendCookie(new HttpCookie("IdToken") { HttpOnly = true, SameSite = SameSiteMode.Strict, Shareable = false, Value = response.IdentityToken });
                    Response.AppendCookie(new HttpCookie("AccessToken") { HttpOnly = true, SameSite = SameSiteMode.Strict, Shareable = false, Value = response.AccessToken });
                    Response.AppendCookie(new HttpCookie("RefreshToken") { HttpOnly = true, SameSite = SameSiteMode.Strict, Shareable = false, Value = response.RefreshToken });
                    Response.AppendCookie(new HttpCookie("ExpiresAt") { HttpOnly = true, SameSite = SameSiteMode.Strict, Shareable = false, Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) });

                    HttpContext.GetOwinContext().Authentication.SignIn(
                        properties,
                        new System.Security.Claims.ClaimsIdentity(user.Identity, user.Claims)
                        );
                };

                return parseUserInfoAndLogin(userName, firstName, lastName, 
                    groups, customClaims,
                    redirectUrl, addOpenIdTokenToContext);
            }
            catch (Exception ex)
            {
                //nothing
            }

            // If you got this far, then something was invalid.
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [AllowAnonymous, Route("sso"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult ParseFormsResponse(LoginModel model, string ReturnUrl)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
          
            if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.ToUpper() == "/RESET")
            {
                ReturnUrl = "";
            }

            if (ModelState.IsValid)
            {
                var resource = Community.ValidateResource(model.UserName, model.Password);
                if (resource != null)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, false);
                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        Uri testUri;
                        Uri.TryCreate(ReturnUrl, UriKind.RelativeOrAbsolute, out testUri);

                        if (testUri.IsAbsoluteUri)
                        {
                            ReturnUrl = "/home";
                        }

                        return Redirect(Server.UrlDecode(ReturnUrl));
                    }
                    else
                    {
                        return Redirect("/home");
                    }
                }
                else
                {
                    ModelState.AddModelError(OthersMessages.Unauthorized, OthersMessages.IncorrectPassword);
                    return View("Login", model);
                }
            }

            ModelState.AddModelError(ApiMessages.UnknownError, UNKNOWN_ERROR_MESSAGE);
            return View("Login", model);
        }

        [AllowAnonymous, Route("slo-callback")]
        public ActionResult LogoutCallback()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
            return View("Logout");
        }

        [Route("slo")]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();  // Logout locally.

            switch (Community.CurrentCompanySsoModel.AuthenticationType)
            {
                case AuthenticationType.OpenId:
                    var authenticationSettings = Community.CurrentCompanySsoModel.StructuredAuthenticationSettings;

                    var idToken = Request.Cookies["IdToken"].Value;

                    HttpContext.GetOwinContext().Authentication.SignOut();

                    if (!string.IsNullOrEmpty(idToken))
                    {
                        var callbackUri = $"{Request.Url.Scheme}://{Request.Url.Authority}/slo-callback";
                        var ru = new RequestUrl($"{authenticationSettings.baseUri}/logout");
                        var extras = new Parameters();
                        if (authenticationSettings.extraParameters.Properties().Count() > 0)
                        {
                            foreach (var p in authenticationSettings.extraParameters.Properties())
                            {
                                extras.Add(p.Name, p.Value<string>(p.Name));
                            }
                        }
                        var url = ru.CreateEndSessionUrl(idToken, 
                            callbackUri,
                            extra: extras);

                        return Redirect(url);
                    }
                    break;
                case AuthenticationType.Saml:
                    var sloEndpoint = Community.CurrentCompanySsoModel.IdpSloEndpoint + "";
                    sloEndpoint = sloEndpoint.Trim();
                    if (!string.IsNullOrEmpty(sloEndpoint))
                    {
                        var resource = Community.GetById<Resource>(Community.CurrentResourceID);

                        var lr = new ComponentSpace.SAML2.Protocols.LogoutRequest
                        {
                            NameID = new NameID(resource.Username, APP_ID, APP_ID, "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress", APP_ID),
                            Issuer = new Issuer(APP_ID)
                        };

                        //check for Sso SessionID if present stick in logout.                                                
                        if (Request.Cookies[SessionIndexCookieName] != null)
                        {
                            lr.SessionIndexes = new List<SessionIndex> { new SessionIndex(Request.Cookies[SessionIndexCookieName].Value) };
                        }

                        var lrXml = lr.ToXml();

                        // Send the logout response over HTTP redirect.                        
                        SingleLogoutService.SendLogoutRequestByHTTPRedirect(Response, sloEndpoint, lrXml, null, null);
                    }
                    break;
                default:
                    break;
            }

            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
            return View("Logout");

        }

        bool setTermsOfUseText(OrganizationRegistration registration, RegisterModel model)
        {
            var success = true;

            var termsOfUses = Company.Filter<Contract>(i => i.ContractType == ContractType.ResourceTermsOfUse && (!i.OrganizationID.HasValue || i.OrganizationID == registration.OrganizationID)).ToList();

            var termsOfUseToDisplay = termsOfUses.Where(i => i.OrganizationID.HasValue).ToList();

            if (termsOfUseToDisplay == null || termsOfUseToDisplay.Count == 0)
            {
                termsOfUseToDisplay = termsOfUses.Where(i => !i.OrganizationID.HasValue).ToList();
            }

            if (termsOfUseToDisplay == null || termsOfUseToDisplay.Count == 0)
            {
                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoTermOfAgreementFound);
                success = false;
            }

            model.Contracts = termsOfUseToDisplay.Select(s => new ContractRegisterModel(s)).ToList();
            model.Step = RegisterStep.TermsOfUse;

            return success;
        }

        bool setOrgAndUserTermsOfUseText(OrganizationRegistration registration, RegisterModel model)
        {
            var success = true;

            var termsOfUses = Company.Filter<Contract>(i => (!i.OrganizationID.HasValue || i.OrganizationID == registration.OrganizationID)).ToList();

            var termsOfUseToDisplay = termsOfUses.Where(i => i.OrganizationID.HasValue).ToList();

            if (termsOfUseToDisplay == null || termsOfUseToDisplay.Count == 0)
            {
                termsOfUseToDisplay = termsOfUses.Where(i => !i.OrganizationID.HasValue).ToList();
            }

            if (termsOfUseToDisplay == null || termsOfUseToDisplay.Count == 0)
            {
                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoTermOfAgreementFound);
                success = false;
            }

            model.Contracts = termsOfUseToDisplay.Select(s => new ContractRegisterModel(s)).ToList();
            model.Step = RegisterStep.TermsOfUse;

            return success;
        }

        [AllowAnonymous, Route("registration")]
        public async Task<ActionResult> Registration()
        {
            return await Register(null, RegisterStep.Registration).ConfigureAwait(false);
        }

        [AllowAnonymous, Route("register")]
        public async Task<ActionResult> Register(Guid? registrationId = null, RegisterStep startStep = RegisterStep.Initial)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary()); 

            var model = new RegisterModel { Step = startStep, RegistrationID = registrationId, Accept = false };
            model.IsUsingActiveDirectory = isUsingActiveDirectory();
            var orgs = Company.Filter<Organization>(i => i.AdministratorEmail == model.Email && (i.Accepted ?? false) == false && i.State == State.Active);

            if (registrationId.HasValue)
            {
                var registration = Company.GetById<OrganizationRegistration>(registrationId.Value);
                if (registration != null)
                {
                    model.Step = registration.Step;

                    if (registration.RegisteredCompletedOn.HasValue)
                    {
                        model.Message = "You have already registered.";
                    }
                    model.Email = registration.Email ?? " ";

                    switch (registration.Step)
                    {
                        case RegisterStep.TermsOfUse:
                            if (orgs.Any())
                            {
                                setOrgAndUserTermsOfUseText(registration, model);
                            }
                            else
                            {
                                setTermsOfUseText(registration, model);
                            }
                            break;
                        case RegisterStep.TermsOfUseValidated:
                            if (orgs.Any())
                            {
                                foreach (var o in orgs)
                                {
                                    o.Accepted = true;
                                    o.AcceptedBy = Company.CurrentResourceID;
                                    o.DateAccepted = DateTime.UtcNow;
                                }
                                Company.SaveChanges();
                            }

                            model.Message = "Thank you for accepting the terms of use. You may now <a href='/'>sign into Data360</a>.";
                            break;
                    }

                }
                else
                {
                    model.Message = "No registration found.";
                }
            }

            return await Task.Run(() => View("Register", model));
        }

        private async Task<InvitedUserResult> registerAzureActiveDirectoryGuest(string email, string firstName, string lastName, string title, string url)
        {            
            var settings = SettingsRepository.GetSettingsAsDictionary();
            var tenantId = settings["AzureADTenant"];     //ad tenant / directory id
            var clientSecret = settings["AzureGraphAPIKey"]; // key for application from azure portal
            var clientId = settings["AzureApplicationId"]; //application id from azure portal

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
            {
                var invite = await AzureGraphProvider.CreateGuestAccount(email, firstName, lastName, title, url, tenantId, clientId, clientSecret);

                return invite;
            }

            return null;
        }

        [AllowAnonymous, Route("register"), HttpPost]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            model.IsUsingActiveDirectory = isUsingActiveDirectory();
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());

            if (ModelState.IsValid)
            {
                model.Email = model.Email.Trim();

                switch (model.Step)
                {
                    case RegisterStep.Initial:
                        #region
                        System.Net.Mail.MailAddress address = null;
                        try
                        {

                            address = new System.Net.Mail.MailAddress(model.Email);

                            var emailDomain = address.Host;

                            if (string.IsNullOrEmpty(emailDomain))
                            {
                                ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.NoEmailDomainResolved);
                                return View(model);
                            }

                            emailDomain = emailDomain.Trim();

                            var domain = Company.OrganizationDomains.FirstOrDefault(d => d.Domain == emailDomain);
                            var orgs = Company.Filter<Organization>(i => i.AdministratorEmail == model.Email && (i.Accepted ?? false) == false && i.State == State.Active);


                            if (orgs.Any())
                            {
                                //GOOD TO GO
                                var registration = new OrganizationRegistration
                                {
                                    Email = model.Email,
                                    ID = Guid.NewGuid(),
                                    OrganizationID = orgs.First().ID,
                                    RegisteredStartedOn = DateTime.UtcNow,
                                    Step = RegisterStep.Registration
                                };
                                Company.Add(registration);

                                if (model.IsUsingActiveDirectory)
                                {
                                    model.Step = RegisterStep.ADRegistration;
                                    model.RegistrationID = registration.ID;
                                }
                                else
                                {

                                    var content = $@"Please complete registration to {orgs.First().Name} by entering the following code:<br/><br/><strong>{registration.ID}</strong>";
                                    await Mail.SendMessage("Data360 Registration", "Complete your registration", model.Email, model.Email, content, true);

                                    model.Step = RegisterStep.Email;
                                    model.Message = "You will receive an email shortly to confirm ownership of this email address, and to continue registration.";
                                }

                                return View(model);

                            }
                            else if (domain != null)
                            {
                                var org = Company.GetById<Organization>(domain.OrganizationID);

                                //GOOD TO GO
                                var registration = new OrganizationRegistration
                                {
                                    Email = model.Email,
                                    ID = Guid.NewGuid(),
                                    OrganizationID = domain.OrganizationID,
                                    RegisteredStartedOn = DateTime.UtcNow,
                                    Step = RegisterStep.Registration
                                };
                                Company.Add(registration);

                                if (model.IsUsingActiveDirectory)
                                {
                                    model.Step = RegisterStep.ADRegistration;
                                    model.RegistrationID = registration.ID;
                                }
                                else
                                {

                                    var content = $@"Please complete registration to {org.Name} by entering the following code:<br/><br/><strong>{registration.ID}</strong>";
                                    await Mail.SendMessage("Data360 Registration", "Complete your registration", model.Email, model.Email, content, true);

                                    model.Step = RegisterStep.Email;
                                    model.Message = "You will receive an email shortly to confirm ownership of this email address, and to continue registration.";
                                }

                                return View(model);

                            }
                            else
                            {
                                var invite = Company.OrganizationInvitationDetails.FirstOrDefault(i => i.Email == model.Email);


                                if (invite != null)
                                {
                                    //GOOD TO GO
                                    var registration = new OrganizationRegistration
                                    {
                                        Email = model.Email,
                                        ID = Guid.NewGuid(),
                                        OrganizationID = invite.OrganizationID,
                                        RegisteredStartedOn = DateTime.UtcNow,
                                        Step = RegisterStep.Registration
                                    };
                                    Company.Add(registration);

                                    if (model.IsUsingActiveDirectory)
                                    {
                                        model.Step = RegisterStep.ADRegistration;
                                        model.RegistrationID = registration.ID;
                                    }
                                    else
                                    {
                                        var content = $@"Please complete registration to {invite.OrganizationName} by entering the following code:<br/><br/><strong>{registration.ID}</strong>";
                                        await Mail.SendMessage("Data360 Registration", "Complete your registration", model.Email, model.Email, content, true);

                                        model.Step = RegisterStep.Email;
                                        model.Message = OthersMessages.OwnershipConfirmationMail;
                                    }

                                    return View(model);
                                }
                                else
                                {
                                    ModelState.AddModelError( OthersMessages.Unauthorized, OthersMessages.OrganisationNotYetRegistered);
                                    return View(model);
                                }

                            }
                        }
                        catch (Exception)
                        {
                            ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.InvalidEmail);
                            return View(model);
                        }

                    #endregion
                    case RegisterStep.ADRegistration:

                        if (!model.RegistrationID.HasValue)
                        {
                            ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                            return View(model);
                        }

                        {

                            var registration = Company.GetById<OrganizationRegistration>(model.RegistrationID.Value);
                            if (registration != null)
                            {
                                model.Email = registration.Email;

                                #region Check/Create resource account in community

                                var resource = Community.Filter<Resource>(i => i.Email == model.Email, i => i.CompanyResources).SingleOrDefault();

                                if (resource == null)
                                {
                                    resource = new Resource
                                    {
                                        Email = model.Email,
                                        FirstName = model.FirstName,
                                        LastName = model.LastName,
                                        Password = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                                        Username = model.Email
                                    };
                                    Community.Add(resource);

                                    Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                }
                                else
                                {
                                    if (!resource.CompanyResources.Any(i => i.CompanyID == Community.CurrentCompanyID))
                                    {
                                        Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                    }
                                }

                                {
                                    var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                    if (orgResource == null)
                                    {
                                        orgResource = new OrganizationResource { Accepted = false, OrganizationID = registration.OrganizationID, ResourceID = resource.ID };
                                        Company.Add(orgResource);
                                    }
                                }


                                if (resource == null)
                                {
                                    ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.ResourceNotThisUser);
                                    return View(model);
                                }

                                #endregion

                                #region Check if organization resource account exists

                                var org = Company.Filter<Organization>(i => i.AdministratorEmail == model.Email && (i.Accepted ?? false) == false && i.State == State.Active);

                                if (org.Any())
                                {
                                    foreach (var o in org)
                                    {
                                        o.Accepted = true;
                                        o.AcceptedBy = resource.ID;
                                        o.DateAccepted = DateTime.UtcNow;
                                    }
                                    Company.SaveChanges();
                                }
                                else
                                {
                                    var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                    if (orgResource == null)
                                    {
                                        ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.ResourceNotSetOrgResource);
                                        return View(model);
                                    }

                                    orgResource.Accepted = true;
                                    orgResource.DateAccepted = DateTime.UtcNow;
                                    Company.Update(orgResource);
                                }

                                #endregion

                                // Save current place in the process.
                                registration.Step = RegisterStep.TermsOfUseValidated;
                                Company.Update(registration);

                                if (model.IsUsingActiveDirectory)
                                {
                                    var aadReturnDomain = Company.CurrentCompanyDomain;

                                    var host = Request.Headers["Host"];
                                    if (host.Contains(".data3sixty"))
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}.data3sixty.com/";
                                    }
                                    else
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}/";
                                    }

                                    var inviteResult = await registerAzureActiveDirectoryGuest(model.Email, model.FirstName, model.LastName, model.Title, aadReturnDomain);

                                    if (inviteResult != null && !string.IsNullOrEmpty(inviteResult.inviteRedeemUrl))
                                    {
                                        return new RedirectResult(inviteResult.inviteRedeemUrl);
                                    }

                                    model.Message = "Thank you registering. Please review your mail for an invitation to use Data360.";
                                }
                                else
                                {
                                    model.Message = "Thank you for completing registration. You may now <a href='/'>sign into Data360</a>.";
                                }

                                model.Step = registration.Step;
                                return View(model);
                            }
                            else
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                                return View(model);
                            }
                        }

                    case RegisterStep.Registration:
                        #region
                        try
                        {
                            #region Validation

                            if (!model.RegistrationID.HasValue)
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                                return View(model);
                            }

                            if (!model.Password.Equals(model.ConfirmPassword))
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.PasswordNotMatch);
                                return View(model);
                            }

                            if (!Regex.Match(model.Password, Resources.Validation.Password_Regex).Success)
                            {
                                ModelState.AddModelError(ApiMessages.Invalid,string.Format(OthersMessages.NotMeetPasswordRule,Resources.Validation.Password_Requirements));
                                return View(model);
                            }

                            #endregion

                            var registration = Company.GetById<OrganizationRegistration>(model.RegistrationID.Value);
                            if (registration != null)
                            {
                                model.Email = registration.Email;

                                #region Check/Create resource account in community

                                var resource = Community.Filter<Resource>(i => i.Email == model.Email, i => i.CompanyResources).SingleOrDefault();

                                if (resource == null)
                                {
                                    resource = new Resource
                                    {
                                        Email = model.Email,
                                        FirstName = model.FirstName,
                                        LastName = model.LastName,
                                        Password = PasswordHelper.HashPassword(model.Password),
                                        Username = model.Email
                                    };
                                    Community.Add(resource);

                                    Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                }
                                else
                                {
                                    if (!resource.CompanyResources.Any(i => i.CompanyID == Community.CurrentCompanyID))
                                    {
                                        Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                    }
                                }

                                #endregion

                                #region Check/create organization resource

                                var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                if (orgResource == null)
                                {
                                    orgResource = new OrganizationResource { Accepted = false, OrganizationID = registration.OrganizationID, ResourceID = resource.ID };
                                    Company.Add(orgResource);
                                }


                                #endregion

                                // Save current place in the process.
                                //TOU is handled after login now, so skip those steps
                                registration.Step = RegisterStep.TermsOfUseValidated;
                                Company.Update(registration);
                                model.Step = registration.Step;
                                model.Message = "Thank you for completing registration. You may now <a href='/'>sign into Data360</a>.";

                                return View(model);
                            }
                            else
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                                return View(model);
                            }
                        }
                        catch (Exception)
                        {
                            ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.InvalidEmail);
                            return View(model);
                        }

                    #endregion
                    case RegisterStep.ADTermsOfUse:
                        try
                        {
                            #region Validation

                            if (!model.RegistrationID.HasValue)
                            {
                                ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.NoRegistrationFound);
                                return View(model);
                            }

                            #endregion

                            var registration = Company.GetById<OrganizationRegistration>(model.RegistrationID.Value);
                            if (registration != null)
                            {
                                #region Check/Create resource account in community

                                var resource = Community.Filter<Resource>(i => i.Email == model.Email, i => i.CompanyResources).SingleOrDefault();

                                if (resource == null)
                                {
                                    resource = new Resource
                                    {
                                        Email = model.Email,
                                        FirstName = model.FirstName,
                                        LastName = model.LastName,
                                        Password = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                                        Username = model.Email
                                    };
                                    Community.Add(resource);

                                    Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                }
                                else
                                {
                                    if (!resource.CompanyResources.Any(i => i.CompanyID == Community.CurrentCompanyID))
                                    {
                                        Community.Add(new CompanyResource { CompanyID = Community.CurrentCompanyID, IsAdministrator = false, State = CompanyResourceState.Active, ResourceID = resource.ID });
                                    }
                                }

                                {
                                    var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                    if (orgResource == null)
                                    {
                                        orgResource = new OrganizationResource { Accepted = false, OrganizationID = registration.OrganizationID, ResourceID = resource.ID };
                                        Company.Add(orgResource);
                                    }
                                }


                                if (resource == null)
                                {
                                    ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.ResourceNotThisUser );
                                    return View(model);
                                }

                                #endregion

                                #region Check if organization resource account exists

                                var org = Company.Filter<Organization>(i => i.AdministratorEmail == model.Email && (i.Accepted ?? false) == false && i.State == State.Active);

                                if (org.Any())
                                {
                                    foreach (var o in org)
                                    {
                                        o.Accepted = true;
                                        o.AcceptedBy = resource.ID;
                                        o.DateAccepted = DateTime.UtcNow;
                                    }
                                    Company.SaveChanges();
                                }
                                else
                                {
                                    var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                    if (orgResource == null)
                                    {
                                        ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.ResourceNotSetOrgResource);
                                        return View(model);
                                    }

                                    orgResource.Accepted = true;
                                    orgResource.DateAccepted = DateTime.UtcNow;
                                    Company.Update(orgResource);
                                }

                                #endregion

                                // Save current place in the process.
                                registration.Step = RegisterStep.TermsOfUseValidated;
                                Company.Update(registration);

                                if (model.IsUsingActiveDirectory)
                                {
                                    var aadReturnDomain = Company.CurrentCompanyDomain;

                                    var host = Request.Headers["Host"];
                                    if (host.Contains(".data3sixty"))
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}.data3sixty.com/";
                                    }
                                    else
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}/";
                                    }

                                    var inviteResult = await registerAzureActiveDirectoryGuest(model.Email, model.FirstName, model.LastName, model.Title, aadReturnDomain);

                                    if (inviteResult != null && !string.IsNullOrEmpty(inviteResult.inviteRedeemUrl))
                                    {
                                        return new RedirectResult(inviteResult.inviteRedeemUrl);
                                    }

                                    model.Message = "Thank you registering. Please review your mail for an invitation to use Data360.";
                                }
                                else
                                {
                                    model.Message = "Thank you for completing registration. You may now <a href='/'>sign into Data360</a>.";
                                }

                                model.Step = registration.Step;
                                return View(model);
                            }
                            else
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                                return View(model);
                            }
                        }
                        catch (Exception)
                        {
                            ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.InvalidEmail);
                            return View(model);
                        }
                    case RegisterStep.TermsOfUse:
                        #region
                        try
                        {
                            #region Validation

                            if (!model.RegistrationID.HasValue)
                            {
                                ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.NoRegistrationFound);
                                return View(model);
                            }

                            #endregion

                            var registration = Company.GetById<OrganizationRegistration>(model.RegistrationID.Value);
                            if (registration != null)
                            {
                                #region Validation

                                if (!model.Accept ?? false)
                                {
                                    ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.MustAcceptTermsOfUse);
                                    return View(model);
                                }

                                var resource = Community.Filter<Resource>(i => i.Email == model.Email, i => i.CompanyResources).SingleOrDefault();
                                if (resource == null)
                                {
                                    ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.NoResourceAccount);
                                    return View(model);
                                }
                                else
                                {
                                    if (!resource.CompanyResources.Any(i => i.CompanyID == Community.CurrentCompanyID))
                                    {
                                        ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.ResoourceAccountNotAllocatedToCompany);
                                        return View(model);
                                    }
                                }

                                #endregion

                                #region Check if organization resource account exists

                                var org = Company.Filter<Organization>(i => i.AdministratorEmail == model.Email && (i.Accepted ?? false) == false && i.State == State.Active);

                                if (org.Any())
                                {
                                    foreach (var o in org)
                                    {
                                        o.Accepted = true;
                                        o.AcceptedBy = Company.CurrentResourceID;
                                        o.DateAccepted = DateTime.UtcNow;
                                    }
                                    Company.SaveChanges();
                                }
                                else
                                {

                                    var orgResource = Company.Filter<OrganizationResource>(i => i.ResourceID == resource.ID && i.OrganizationID == registration.OrganizationID).SingleOrDefault();

                                    if (orgResource == null)
                                    {
                                        ModelState.AddModelError(ApiMessages.Invalid,OthersMessages.ResourceNotSetOrgResource);
                                        return View(model);
                                    }

                                    orgResource.Accepted = true;
                                    orgResource.DateAccepted = DateTime.UtcNow;
                                    Company.Update(orgResource);
                                }


                                #endregion

                                // Save current place in the process.
                                registration.Step = RegisterStep.TermsOfUseValidated;
                                Company.Update(registration);

                                if (model.IsUsingActiveDirectory)
                                {
                                    var aadReturnDomain = Company.CurrentCompanyDomain;

                                    var host = Request.Headers["Host"];
                                    if (host.Contains(".data3sixty"))
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}.data3sixty.com/";
                                    }
                                    else
                                    {
                                        aadReturnDomain = $"https://{aadReturnDomain}/";
                                    }

                                    await registerAzureActiveDirectoryGuest(model.Email, model.FirstName, model.Title, model.LastName, aadReturnDomain);
                                    model.Message = "Thank you for accepting the terms of use.  Please review your mail for an invitation to use Data360.";
                                }
                                else
                                {
                                    model.Message = "Thank you for accepting the terms of use. You may now <a href='/'>sign into Data360</a>.";
                                }

                                model.Step = registration.Step;
                                return View(model);
                            }
                            else
                            {
                                ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.NoRegistrationFound);
                                return View(model);
                            }
                        }
                        catch (Exception)
                        {
                            ModelState.AddModelError(ApiMessages.Invalid, OthersMessages.InvalidEmail);
                            return View(model);
                        }

                    #endregion
                    case RegisterStep.TermsOfUseValidated:
                        #region

                        break;
                        #endregion
                }
            }

            ModelState.AddModelError(ApiMessages.UnknownError, UNKNOWN_ERROR_MESSAGE);
            return View(model);
        }

        private bool isUsingActiveDirectory()
        {
            // check that we are single sign on if not return false
            var c = Community.GetById<Company>(Company.CurrentCompanyID, i => i.CompanyDomainSettings);

            foreach (var companySetting in c.CompanyDomainSettings)
            {
                if (Company.CurrentCompanyDomain == companySetting.UrlPrefix)
                {
                    if (companySetting.AuthenticationType != AuthenticationType.Saml)
                    {
                        return false;
                    }
                    break;
                }
            }

            // now check if we also have the required ad guest info.
            var settings = SettingsRepository.GetSettingsAsDictionary();
            var tenantId = settings["AzureADTenant"];     //ad tenant / directory id
            var clientSecret = settings["AzureGraphAPIKey"]; // key for application from azure portal
            var clientId = settings["AzureApplicationId"]; //application id from azure portal

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            return true;
        }

        [AllowAnonymous, Route("reset"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reset(LoginModel model)
        {
            //add record with guid that the user requested password reset
            var resource = Company.GlobalReportingResources.Where(x => x.Email == model.UserName).FirstOrDefault();

            if (resource != null)
            {
                // delete any pending requests for this resource id

                var pending = Company.ResourcePasswordResets.Where(x => x.ResourceID == resource.ResourceID);

                if (pending.Any())
                {
                    Company.ResourcePasswordResets.RemoveRange(pending);
                    Company.SaveChanges();
                }

                // add record for password reset request
                var resetModel = new ResourcePasswordReset
                {
                    CreateDate = DateTime.UtcNow,
                    ResourceID = resource.ResourceID
                };

                Company.ResourcePasswordResets.Add(resetModel);
                Company.SaveChanges();

                //send email with link
                var templateValues = new Dictionary<string, string>();

                string strUrl = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/");
                strUrl += $"doreset?id={resetModel.ID}";

                templateValues["firstname"] = resource.FirstName;
                templateValues["request_url"] = strUrl;

                //email user 
                Mail.SendMessage("Data360 Forgotten Password", resource.Email, resource.FullName, templateValues, "forgot-password-reset-request");
            }
            //redirect to login page
            FormsAuthentication.RedirectToLoginPage();
            return new EmptyResult();
        }

        [AllowAnonymous, Route("reset")]
        public ActionResult Reset()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
            return View("Reset");
        }

        [AllowAnonymous, Route("doreset")]
        public ActionResult DoReset()
        {
            var id = Request.QueryString["id"];

            if (!string.IsNullOrEmpty(id))
            {
                Guid guidId = Guid.Empty;

                if (Guid.TryParse(id, out guidId))
                {
                    var resetRequest = Company.ResourcePasswordResets.Where(x => x.ID == guidId).FirstOrDefault();

                    if (resetRequest != null)
                    {
                        var resource = Company.GlobalReportingResources.Where(x => x.ResourceID == resetRequest.ResourceID).FirstOrDefault();
                        if (resource != null)
                        {
                            bool success = false;
                            // check that the request is less then 24 hours old
                            if ((resetRequest.CreateDate - DateTime.UtcNow).TotalDays < 1)
                            {
                                ResetResourcePassword(resource.ResourceID, resource.FirstName, resource.Email, resource.FullName);
                                success = true;
                            }

                            Company.ResourcePasswordResets.Remove(resetRequest);
                            Company.SaveChanges();

                            if (success)
                            {
                                ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
                                ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
                                return View("ResetMessage");
                            }
                        }
                    }
                }
            }

            FormsAuthentication.RedirectToLoginPage();
            return new EmptyResult();
        }

        [AllowAnonymous, Route("noaccess")]
        public ActionResult NoAccess()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            return View("NoAccess");
        }

        [AllowAnonymous, Route("Error")]
        public ActionResult Error()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            return View("../Shared/GenericError");
        }

        [AllowAnonymous, Route("inactive-company")]
        public ActionResult InactiveCompany()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            return View("InactiveCompany");
        }

    }
}
