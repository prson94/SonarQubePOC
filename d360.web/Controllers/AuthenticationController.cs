using ComponentSpace.SAML2.Assertions;
using ComponentSpace.SAML2.Profiles.SingleLogout;
using ComponentSpace.SAML2.Profiles.SSOBrowser;
using ComponentSpace.SAML2.Protocols;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.extensions;
using d360.extensions.caching;
using d360.web.caching;
using d360.web.Extensions;
using d360.web.Models;
using Dapper;
using IdentityModel.Client;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Resources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;

namespace d360.web.Controllers
{
	[RoutePrefix("")]
    public class AuthenticationController : BaseController
    {
        private const string APP_ID = "https://data3sixty.com/ui";
        private const string SessionIndexCookieName = "SessionIndex";

		#region DI

		private readonly OidcDiscoveryCache Discovery;
		private readonly TelemetryClient Telemetry;

        public AuthenticationController(ICoreComponentSet set, IMailProvider mail, OidcDiscoveryCache discovery)
            : base(set)
        {
            Mail = mail;
            Telemetry = new TelemetryClient();
            Telemetry.Context.InstrumentationKey = ConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            Telemetry.Context.GlobalProperties["CompanyID"] = Company.CurrentCompanyID.ToString();

			Discovery = discovery;
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

        private Parameters loadExtraParametersFromOpenIdSettings(CompanyOpenIdAuthenticationSettings authenticationSettings)
        {
            Parameters extras = null;

            if (authenticationSettings.extraParameters != null && authenticationSettings.extraParameters.Properties() != null)
            {
                if (authenticationSettings.extraParameters.Properties().Count() > 0)
                {
                    extras = new Parameters();

                    foreach (var p in authenticationSettings.extraParameters.Properties())
                    {
                        extras.Add(p.Name, p.Value.ToString());
                    }
                }
            }

            return extras;
        }

        private ActionResult parseUserInfoAndLogin(
            string userName, string firstName, string lastName,
            Dictionary<string,List<string>> groups = null, Dictionary<string, string> customClaims = null,
            string relayState = null,
            System.Action customAction = null)
        {
            Resource resource = null;

            if (!string.IsNullOrEmpty(userName))
            {
                userName = userName.ToLower();
                resource = Community.Filter<Resource>(i => i.Username.ToLower() == userName).SingleOrDefault();
                
                //If there is a domain whitelist, make sure the user has access
                string domainWhitelistString = SettingsRepository.GetSetting(Setting.EmailDomainWhitelist).Value;
                bool isDomainWhitelisted;
                
                //For internal use, bypass the whitelist
                var host = Request.Headers["Host"];
                if (host.Contains("-pcy") || host.Contains("-d3s") || host.Contains("-igx"))
                {
                    isDomainWhitelisted = true;
                }
                else if (!string.IsNullOrWhiteSpace(domainWhitelistString))
                {
                    isDomainWhitelisted = false;
                    var domainWhitelist = domainWhitelistString.Split(',');
                    var userEmail = new MailAddress(userName);
                    var userDomain = userEmail.Host;

                    foreach(var domain in domainWhitelist)
                    {
                        if (string.Equals(userDomain, domain.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                        {
                            isDomainWhitelisted = true;
                            break;
                        }
                    }
					Telemetry.TrackTrace(new TraceTelemetry { Message = $"Environment has domain whitelist. User Domain = {userDomain}, In Whitelist = {isDomainWhitelisted}", SeverityLevel = SeverityLevel.Information });
				}
                else
                {
                    isDomainWhitelisted = true;
                }

                if (isDomainWhitelisted == false)
                {
                    return Redirect("/noaccess");
                }

                // If user is assigned to any groups in SAML claims, then check to see if any of those groups should be assigned as admin. If so, assign the user as admin.
                bool isCompanyAdministrator = false;

                if (groups?.Any() == true)
                {
					List<string> allGroups = new List<string>();
					foreach(var key in groups.Keys)
					{
						allGroups.AddRange(groups[key]);
					}

                    isCompanyAdministrator = Community.CompanyDomainGroups.Any(g =>
                        g.CompanyID == Community.CurrentCompanyID &&
                        g.DomainSettingID == Community.CurrentDomainSettingID &&
						allGroups.Contains(g.GroupName) &&
                        g.IsAdministrator);
					
					Telemetry.TrackTrace(new TraceTelemetry { Message = $"Should user be admin based on group membership = {isCompanyAdministrator}", SeverityLevel = SeverityLevel.Verbose});
				}

                if (resource == null)
                {
                    Telemetry.TrackTrace(new TraceTelemetry { 
						Message = $"Did not find resource account for Username: {userName}. Other info: (First: {firstName}, Last: {lastName}, Allow New Users: {Community.CurrentCompanySsoModel.AllowNewUserLogin})", 
						SeverityLevel = SeverityLevel.Information });

                    if (Community.CurrentCompanySsoModel.AllowNewUserLogin && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"Now creating resource account for Username: {userName}.", SeverityLevel = SeverityLevel.Information });

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

                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"Finished creating resource account for Username: {userName}.", SeverityLevel = SeverityLevel.Verbose });
                    }
                }
                else
                {
                    var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == resource.ID).SingleOrDefault();
                    
                    if (companyResource == null)
                    {
						Telemetry.TrackTrace(new TraceTelemetry
						{
							Message = $"User not associated to tenant. Username: {userName}. Other info: (First: {firstName}, Last: {lastName}, Allow New Users: {Community.CurrentCompanySsoModel.AllowNewUserLogin})",
							SeverityLevel = SeverityLevel.Information
						});

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
						Telemetry.TrackTrace(new TraceTelemetry
						{
							Message = $"Is user active on tenant? {companyResource.State}. Username: {userName}.",
							SeverityLevel = SeverityLevel.Verbose
						});

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
						Telemetry.TrackTrace(new TraceTelemetry
						{
							Message = $"User attempting to log in does not have access. Username: {userName}.",
							SeverityLevel = SeverityLevel.Verbose
						});

						return Redirect("/noaccess");
                    }
                }
            }

            if (resource != null)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"Resource account exists for Username: {userName}. Now authorizing with cookie.", SeverityLevel = SeverityLevel.Verbose });

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
                                    dt.Columns.Add("Origin", typeof(string));

									foreach(var key in groups.Keys)
									{
										groups[key].ForEach(g =>
										{
											var row = dt.NewRow();
											row["Name"] = g.Trim();
											row["Origin"] = string.IsNullOrWhiteSpace(key) ? null : key;
											dt.Rows.Add(row);
										});
									}

                                    sqlBulkCopy.ColumnMappings.Add("Name", "Name");
                                    sqlBulkCopy.ColumnMappings.Add("Origin", "Origin");
                                    sqlBulkCopy.DestinationTableName = "#ADGroups";
                                    sqlBulkCopy.BulkCopyTimeout = 60;

                                    Company.Connection.Execute(
                                        @"drop table if exists #ADGroups;
                                            create table #ADGroups ([Name] nvarchar(max), [Origin] nvarchar(10), GroupID int, HasResourceGroup bit);"
                                    , transaction: trans);

                                    sqlBulkCopy.WriteToServer(dt);

                                    Company.Connection.Execute(
										@"	
											update A
                                            set A.GroupID = G.ID,
                                            HasResourceGroup = case when RG.ResourceID is not null then 1 else null end
                                            from    #ADGroups A
                                                    inner join [Group] G on G.IsActiveDirectoryGroup = 1 and G.[Name] = A.[name]
                                                    left join ResourceGroup RG on RG.GroupID = G.ID and RG.ResourceID = @resourceId

                                            insert into ResourceGroup (ResourceID, GroupID, Origin)
                                            select  @resourceId, GroupID, Origin
                                            from    #ADGroups 
                                            where   GroupID is not null and coalesce(HasResourceGroup, 0) = 0

											update RG
											set RG.Origin = A.Origin
											from ResourceGroup RG
											inner join [Group] G on G.ID = RG.GroupID and G.IsActiveDirectoryGroup = 1
											inner join #ADGroups A on coalesce(A.HasResourceGroup, 0) = 1 and (A.Origin != RG.Origin or RG.Origin is null) and A.GroupID = G.ID
											where RG.ResourceID = @resourceId;

                                            delete  R
                                            from    ResourceGroup R
                                                    inner join [Group] G on G.ID = R.GroupID and G.IsActiveDirectoryGroup = 1
                                            where   R.ResourceID = @resourceId and not exists (select 1 from #ADGroups where GroupID = R.GroupID and (Origin is null or Origin = R.Origin))"
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
						var resourceAssetType = Company.Filter<AssetType>(a => a.Class == AssetTypeClass.User).Select(i => i.ID).ToList();
                        
						var resourceTypeFields = Company.Filter<FieldType>(i => i.AssetTypeID.HasValue && resourceAssetType.Contains(i.AssetTypeID.Value)).ToList();
						
						var resourceAsset = Company.Filter<Asset>(a => a.uid == resource.Uid).FirstOrDefault();
						var resourceFields = Company.Filter<Field>(i => i.AssetID == resourceAsset.ID).ToList();

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
                                rf = new Field { FieldTypeID = f.ID, AssetID = resourceAsset.ID, Value = customClaims[claimName] };
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
                            Message = $"Referencing resource: {resource.ID}. Should not authorize with the system account.  The username is: {userName}",
                            SeverityLevel = SeverityLevel.Error
                        });
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest); //If you made this far, then error occurred.
        }

        [AllowAnonymous, Route("sso")]
        public async Task<ActionResult> Login()
        {
            if (string.Equals(Request?.Browser?.Browser, "internetexplorer", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("unsupported", "home");
            }

            if (!Community.CurrentCompanySsoModel.IsCompanyActive)
            {
                return InactiveCompany();
            }

            string returnUrl = Request.QueryString["ReturnUrl"];

            if (Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out var testUri) == false || testUri.IsAbsoluteUri)
            {
                returnUrl = "/home";
            }

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


					var client = new HttpClient();
					var discoveryUri = string.IsNullOrEmpty(authenticationSettings.discoveryUri) ? authenticationSettings.baseUri : authenticationSettings.discoveryUri;
					var discoDoc = await Discovery.GetDiscoverDocument(client, discoveryUri);
					var authUri = discoDoc.authorization_endpoint ?? $"{authenticationSettings.baseUri}/authorize";
					var ru = new RequestUrl(authUri);

					var scopes = "openid profile email infogix";

					if (authenticationSettings.scopes != null && authenticationSettings.scopes.Count > 0)
					{
						scopes = string.Join(" ", authenticationSettings.scopes);
					}

					var url = ru.CreateAuthorizeUrl(
                        clientId: authenticationSettings.clientId,
                        responseType: "code",
                        scope: scopes,
                        callbackUri,
                        state,
                        nonce,
                        responseMode: "form_post",
                        extra: loadExtraParametersFromOpenIdSettings(authenticationSettings)
                        );

                    return new RedirectResult(url);
                default:    // Login via standard forms authentication.
                    ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
                    await AppendSettingsToViewData();

                    return View();
            }
        }

        [AllowAnonymous, Route("sso/acs"), HttpPost]
        public ActionResult ParseSamlResponse()
        {
            // Extract the asserted identity from the SAML response.
            // The SAML assertion may be signed or encrypted and signed.
            var telemetry = new TelemetryClient();

			Telemetry.Context.Operation.Id = Guid.NewGuid().ToString();
			Telemetry.Context.Operation.Name = "ParseSamlResponse";

			SAMLResponse samlResponse = null;
            ServiceProvider.ReceiveSAMLResponseByHTTPPost(Request, out XmlElement samlResponseXml, out string relayState);

            Telemetry.TrackTrace(new TraceTelemetry { Message = $"samlResponseXml: {samlResponseXml.InnerXml}", SeverityLevel = SeverityLevel.Information });

            // Deserialize the XML.
            samlResponse = new SAMLResponse(samlResponseXml);

            Telemetry.TrackTrace(new TraceTelemetry { Message = $"IsSuccessful: {(samlResponse.IsSuccess() ? "Yes" : "No")}", SeverityLevel = SeverityLevel.Information });

            // Check whether the SAML response indicates success or an error and process accordingly.
            if (samlResponse.IsSuccess())
            {
                SAMLAssertion samlAssertion = null;

                Telemetry.TrackTrace(
                    new TraceTelemetry
                    {
                        Message = $"Assertion Count: {samlResponse.GetAssertions().Count}, Signed Assertion Count: {samlResponse.GetSignedAssertions().Count}, Encrypted Assertion Count: {samlResponse.GetEncryptedAssertions().Count}",
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
				Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>();

                var customClaims = new Dictionary<string, string>();
				var claimMappings = getClaimMappings();

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

					var claim = claimMappings.FirstOrDefault(c => c.Path == attName);
					if (claim != null)
					{
						switch(claim.ClaimType)
						{
							case ClaimType.Email:
								userName = attValue;
								break;
							case ClaimType.FirstName:
								firstName = attValue;
								break;
							case ClaimType.LastName:
								lastName = attValue;
								break;
							case ClaimType.Groups:
								if (!groups.ContainsKey(claim.PathHash))
								{
									groups.Add(claim.PathHash, new List<string>());
								}
								groups[claim.PathHash] = a.Values?.Select(v => v.Data.ToString())?.ToList();
								break;
						}
					}
					else
					{
						customClaims.Add(attName, attValue);
					}
                }

                Telemetry.TrackTrace(new TraceTelemetry { Message = $"SAML Attributes are: {submittedAttributes}", SeverityLevel = SeverityLevel.Verbose });
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"Username: {userName}, FirstName: {firstName}, LastName: {lastName}", SeverityLevel = SeverityLevel.Information });

                System.Action addSamlAssertionToCookie = () =>
                {
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
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"Unsuccessful: {errorMessage}", SeverityLevel = SeverityLevel.Error });

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [AllowAnonymous, Route("sso/openid"), HttpPost]
        public async Task<ActionResult> ParseOpenIdResponse()
        {
            // From IdP.
            var code = Request.Form["code"];
            var state = Request.Form["state"];
			
			Telemetry.Context.Operation.Id = Guid.NewGuid().ToString();
			Telemetry.Context.Operation.Name = "ParseOpenIdResponse";
			Telemetry.TrackTrace(new TraceTelemetry { Message = $"Code = {code}, State = {state}", SeverityLevel = SeverityLevel.Information });

			if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
				Telemetry.TrackException(new ExceptionTelemetry { Message = $"Code and/or State is empty or null.", SeverityLevel = SeverityLevel.Critical });
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ApiMessages.OpenIdCodeOrStateIsNotPresent);
            }

            var authenticationSettings = Community.CurrentCompanySsoModel.StructuredAuthenticationSettings;

			Telemetry.TrackTrace(new TraceTelemetry { Message = $"AuthenticationSettings: {Community.CurrentCompanySsoModel.AuthenticationSettings}", SeverityLevel = SeverityLevel.Information });

			if (string.IsNullOrEmpty(authenticationSettings.baseUri) || string.IsNullOrEmpty(authenticationSettings.clientId) || string.IsNullOrEmpty(authenticationSettings.clientSecret) || string.IsNullOrEmpty(authenticationSettings.audience))
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ApiMessages.MissingConfigInfo);
            }

            var baseUri = authenticationSettings.baseUri;
            var openIdRequest = Community.GetOpenIdRequest(state);

            if (openIdRequest == null)
            {
				Telemetry.TrackException(new ExceptionTelemetry { Message = $"Could not find openIdRequest.", SeverityLevel = SeverityLevel.Error });
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ApiMessages.FailedAuthentication);
            }

            var client = new HttpClient();
            string redirectUri = $"{Request.Url.Scheme}://{Request.Url.Authority}/sso/openid";

			var discoveryUri = string.IsNullOrEmpty(authenticationSettings.discoveryUri) ? baseUri : authenticationSettings.discoveryUri;
			var discoDoc = await Discovery.GetDiscoverDocument(client, discoveryUri);
			var tokenUri = discoDoc.token_endpoint;
			
			var response = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = tokenUri,//$"{baseUri}/token",
				ClientId = authenticationSettings.clientId,
                ClientSecret = authenticationSettings.clientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                Code = code,
                Method = HttpMethod.Post,
                RedirectUri = redirectUri
            });

            if (response.IsError)
            {
				Telemetry.TrackException(new ExceptionTelemetry { Message = $"Got error from RequestAuthorizationCodeTokenAsync.", Exception = response.Exception, SeverityLevel = SeverityLevel.Critical });
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest, response.HttpErrorReason);
            }

			Telemetry.TrackTrace(new TraceTelemetry { Message = $"Token Response: {response.Raw}", SeverityLevel = SeverityLevel.Verbose });

			var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(response.IdentityToken);
			var payload = token.Payload;
			JwtSecurityToken accessToken = null;
			if (!string.IsNullOrEmpty(response.AccessToken))
			{
				accessToken = handler.ReadJwtToken(response.AccessToken);
			}
			var incomingNonce = token.Claims.SingleOrDefault(c => c.Type == "nonce").Value.ToString();
            if (openIdRequest.Nonce != incomingNonce)
            {
				Telemetry.TrackTrace(new TraceTelemetry { Message = $"Nonces do not match: Incoming: {incomingNonce}; Valid: {openIdRequest.Nonce}", SeverityLevel = SeverityLevel.Verbose });
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ApiMessages.FailedAuthenticationNonces);
            }

			var combinedClaims = new List<System.Security.Claims.Claim>();
			combinedClaims.AddRange(token.Claims); // ID token claims.
			if (accessToken != null)
			{
				combinedClaims.AddRange(accessToken.Claims.Except(token.Claims)); // Access token claims.
			}
			var claimMappings = getClaimMappings();
			string redirectUrl = openIdRequest.RedirectUrl;

			var userAuth = new UserAuthentication();
			userAuth.ParseClaims(claimMappings, combinedClaims, payload);

            try
            {
                Community.RemoveOpenIdRequest(openIdRequest);
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string> { { "State", openIdRequest.State } });
            }

            try
            {
                var keySet = await client.GetJsonWebKeySetAsync(discoDoc.jwks_uri);

                var user = response.IdentityToken.ValidateJwtIdentityToken(
					authenticationSettings.nameClaimType,
                    authenticationSettings.audience, false,
                    discoDoc.issuer, (discoDoc.issuer != null),
                    keySet.KeySet.Keys, true, true, true, false);

                System.Action addOpenIdTokenToContext = () =>
                {
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

                return parseUserInfoAndLogin(
					userAuth.Email, userAuth.FirstName, userAuth.LastName,
					userAuth.Groups, userAuth.CustomClaims,
                    redirectUrl, addOpenIdTokenToContext);
            }
            catch (Exception ex)
            {
				Telemetry.TrackException(new ExceptionTelemetry { Message = $"Error when validating JWT token to active user.", Exception = ex, SeverityLevel = SeverityLevel.Critical });
			}

            // If you got this far, then something was invalid.
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

		[AllowAnonymous, Route("sso/openid"), HttpGet]
		public async Task<ActionResult> HandleOpenIdGetResponse()
		{
			return new ContentResult { Content = "Govern does not respond to IdP-initiated JWT requests.", ContentType = "text/html" };
		}

		[AllowAnonymous, Route("sso"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> ParseFormsResponse(LoginModel model, string ReturnUrl)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            await AppendSettingsToViewData();

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
                        Uri.TryCreate(ReturnUrl, UriKind.RelativeOrAbsolute, out Uri testUri);

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
        public async Task<ActionResult> LogoutCallback()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            await AppendSettingsToViewData();

            return View("Logout");
        }

        [Route("slo")]
        public async Task<ActionResult> Logout()
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

						var client = new HttpClient();
						var discoveryUri = string.IsNullOrEmpty(authenticationSettings.discoveryUri) ? authenticationSettings.baseUri : authenticationSettings.discoveryUri;
						var discoDoc = await Discovery.GetDiscoverDocument(client, discoveryUri);

						var endSessionUri = discoDoc.end_session_endpoint ?? $"{authenticationSettings.baseUri}/logout";

						var ru = new RequestUrl(endSessionUri);
                        var url = ru.CreateEndSessionUrl(idToken,
                            callbackUri,
                            extra: loadExtraParametersFromOpenIdSettings(authenticationSettings)
                        );

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
            await AppendSettingsToViewData();

            return View("Logout");
        }

        [AllowAnonymous, Route("reset"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reset(LoginModel model)
        {
            //add record with guid that the user requested password reset
            var resource = Company.GlobalReportingResources.Where(x => x.Email == model.UserName).FirstOrDefault();

            if (resource != null)
            {
                ResourcePasswordReset latest, resetModel;
                bool hasExistingRecord = false;

                //find any pending requests for this resource
                var pending = Company.ResourcePasswordResets.Where(x => x.ResourceID == resource.ResourceID);

                if (pending.Any())
                {
                    latest = pending.OrderByDescending(x => x.CreateDate).First();
                    hasExistingRecord = latest.CreateDate >= DateTime.UtcNow.AddMinutes(-5);

                    //if the most recent record is less than 5 minutes old, leave it and remove the others
                    if (hasExistingRecord)
                    {
                        pending = pending.Where(x => x.ID != latest.ID);
                    }

                    //remove any other pending requests
                    if (pending.Any())
                    {
                        Company.ResourcePasswordResets.RemoveRange(pending);
                        Company.SaveChanges();
                    }
                }

                //if there are no records add one and send the reset email
                if (!hasExistingRecord)
                {
                    var templateValues = new Dictionary<string, string>();
                    string strUrl = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/");

                    //add record for password reset request
                    resetModel = new ResourcePasswordReset
                    {
                        CreateDate = DateTime.UtcNow,
                        ResourceID = resource.ResourceID
                    };

                    Company.ResourcePasswordResets.Add(resetModel);
                    Company.SaveChanges();

                    strUrl += $"doreset?id={resetModel.ID}";

                    //send email with link                   
                    templateValues["firstname"] = resource.FirstName;
                    templateValues["request_url"] = strUrl;

                    //email user 
                    Mail.SendMessage("Data360 Forgotten Password", resource.Email, resource.FullName, templateValues, "forgot-password-reset-request");
                }
            }

            //redirect to login page
            FormsAuthentication.RedirectToLoginPage();

            return new EmptyResult();
        }

        [AllowAnonymous, Route("reset")]
        public async Task<ActionResult> Reset()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            await AppendSettingsToViewData();

            return View("Reset");
        }

        [AllowAnonymous, Route("doreset")]
        public async Task<ActionResult> DoReset()
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
                                await AppendSettingsToViewData();

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

		private List<ClaimMapping> getClaimMappings()
		{
			ICachingProvider cache = new MemoryCachingProvider();
			return cache.GetItem<List<ClaimMapping>>($"{Company.CurrentCompanyID}_{Company.CurrentCompanyDomain}_ClaimMappings") ?? new List<ClaimMapping>();
		}
    }
}
