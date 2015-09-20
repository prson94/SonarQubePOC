using System;
using System.ComponentModel.Composition;
using d360.model;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Linq;
using System.Web.Security;
using d360.core.entities;

namespace d360.extensions.security.windows
{
    [Export(typeof(IAuthenticationSource))]
    public class AuthenticationSource: IAuthenticationSource
    {
        public string CompanyConnectionString
        {
            get;
            set;
        }

        private D360Context setContext()
        {
            return (string.IsNullOrEmpty(CompanyConnectionString)) ?
                    new D360Context() :
                    new D360Context(CompanyConnectionString);
        }


        public bool ChangePassword(int resourceID, string newPassword)
        {
            throw new NotImplementedException();
        }

        public string ResetPassword(int resourceID)
        {
            throw new NotImplementedException();
        }

        public Resource FindAuthenticatedResource(string username)
        {
            var ctx = setContext();
            Resource resource = null;

            try
            {
                resource = ctx.Resources.Include("Company").SingleOrDefault(i => i.Username == username);
            }
            catch
            {
            }
            finally
            {
                if (ctx != null) ctx.Dispose();
            }

            return resource;
        }

        public Resource ValidateResource(string username, string password)
        {
            var ctx = setContext();
            Resource resource = null;

            try
            {
                //if (HttpContext.Current.User == null)
                //    throw new ApplicationException("You must be authenticated to connect to this system.");
                //else
                //{
                    //username = HttpContext.Current.User.Identity.Name.ToLower();
                    resource = ctx.Resources.Include("Company").SingleOrDefault(i => i.Username == username);

                    if (resource != null)
                    {
                        #region Get Domain and Username From userName string.

                        string[] userInfo = username.Split(new string[1] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                        string userInfoDomain = "";
                        string userInfoName = "";
                        if (userInfo.Length == 2)
                        {
                            userInfoDomain = userInfo[0];
                            userInfoName = userInfo[1];
                        }
                        else
                        {
                            userInfoDomain = System.Environment.MachineName;
                            userInfoName = username;
                        }

                        #endregion

                        PrincipalContext domainContext = null;
                        try
                        {
                            domainContext = new PrincipalContext(ContextType.Domain, userInfoDomain);
                        }
                        catch
                        {
                            domainContext = new PrincipalContext(ContextType.Machine, userInfoDomain);
                        }

                        // This is how to authenticate against the domain.
                        if (domainContext.ValidateCredentials(userInfoName, password))
                        {
                            // Authenticated.
                            FormsAuthentication.SetAuthCookie(username, false);
                            FormsAuthentication.RedirectFromLoginPage(username, false);
                        }
                    }
                    else
                    {
                    //    resource = new Resource();

                        //var user = UserPrincipal.FindByIdentity(domainContext, username);

                        //resource.Email = (user.EmailAddress != null) ? user.EmailAddress : "";
                        //try { resource.FirstName = user.GivenName + ""; }
                        //catch { resource.FirstName = ""; }
                        //try { resource.LastName = user.Surname + ""; }
                        //catch { resource.LastName = ""; }
                        //resource.Password = "This is a Windows account!";
                        //resource.Role = ctx.Roles.Single(i => i.Name == "Member");
                        //resource.StatusID = 1;
                        //resource.Username = username.ToLower();
                        //ctx.Resources.Add(resource);
                        //ctx.SaveChanges();
                    }
                //}
            }
            catch
            {
            }
            finally 
            {
                if (ctx != null) ctx.Dispose();
            }

            return resource;
        }

        public void Signout(string username)
        {
            FormsAuthentication.SignOut();
        }
    }
}
