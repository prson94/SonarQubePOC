using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Web.Security;
using d360.model;
using d360.core.communityhub;
using System.Web.Configuration;
using d360.utility;
using System.Data;

namespace d360.extensions.security.forms.communityhub
{
    [ExceptionWrapper(typeof(EntityException))]
    [Export(typeof(IAdminAuthenticationSource))]
    public class AuthenticationSource : IAdminAuthenticationSource
    {
        public string CompanyConnectionString
        {
            get;
            set;
        }

        int _MinimumPasswordLength = 7;
        public int MinimumPasswordLength
        {
            get { return _MinimumPasswordLength; }
            set { _MinimumPasswordLength = value; }
        }

        int _MinimumNonAlphanumericLength = 0;
        public int MinimumNonAlphanumericLength
        {
            get { return _MinimumNonAlphanumericLength; }
            set { _MinimumNonAlphanumericLength = value; }
        }

        #region Utilities

        private bool checkPasswordRequirements(string password)
        {
            bool success = false;

            // Make sure that the password adheres to minimum length.
            if (password.Length >= MinimumPasswordLength)
            {
                // Make sure that the password has the minimum # of non-alphanumerics.
                MatchCollection matches = Regex.Matches(password, @"[^\w]+");
                if (matches.Count >= MinimumNonAlphanumericLength)
                {
                    return true;
                }
            }

            return success;
        }

        public string createRandomPassword()
        {
            string _allowedNonAlphaNumericChars = "!#$%";
            string _allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789" + _allowedNonAlphaNumericChars;
            Random randNum = new Random();
            char[] chars = new char[MinimumPasswordLength];

            for (int i = 0; i < MinimumPasswordLength; i++)
            {
                chars[i] = _allowedChars[(int)((_allowedChars.Length) * randNum.NextDouble())];
            }

            return new string(chars);
        }

        private string processPasswordForStorage(string password)
        {
            password = FormsAuthentication.HashPasswordForStoringInConfigFile(password, FormsAuthPasswordFormat.SHA1.ToString());
            return password;
        }

        private D360Context setContext()
        {
            return (string.IsNullOrEmpty(CompanyConnectionString)) ?
                    new D360Context() :
                    new D360Context(CompanyConnectionString);
        }

        #endregion

        public bool ChangePassword(int id, string newPassword)
        {
            var success = false;

            var ctx = new CommunityHubContext();
            var account = ctx.Administrators.Find(id);
            if (checkPasswordRequirements(newPassword))
            {
                account.Password = processPasswordForStorage(newPassword);
                ctx.SaveChanges();
                success = true;
            }

            return success;
        }

        public string ResetPassword(int id)
        {
            string newPassword = string.Empty;

            var ctx = new CommunityHubContext();
            var account = ctx.Administrators.Find(id);
            if (account != null)
            {
                throw new ApplicationException("No resource found.");
            }
            newPassword = createRandomPassword();
            if (checkPasswordRequirements(newPassword))
            {
                account.Password = processPasswordForStorage(newPassword);
                ctx.SaveChanges();
            }

            return newPassword;
        }

        public Administrator FindAuthenticatedResource(string username)
        {
            var ctx = new CommunityHubContext();
            return ctx.Administrators.SingleOrDefault(i => i.Username == username);
        }

        public Administrator ValidateResource(string username, string password)
        {
            var ctx = new CommunityHubContext();
            password = processPasswordForStorage(password);
            var account = ctx.Administrators.SingleOrDefault(i => i.Username == username && i.Password == password);
            
            if (account != null)
            {
                FormsAuthentication.SetAuthCookie(account.Username, true);
                FormsAuthentication.RedirectFromLoginPage(account.Username, false);
            }

            return account;
        }
        public void Signout(string username)
        {
            FormsAuthentication.SignOut();
        }
    }
}
