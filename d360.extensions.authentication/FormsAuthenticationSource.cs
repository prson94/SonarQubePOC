using System;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Security;
using System.Linq;
using d360.core.entities;

using d360.model;

namespace d360.extensions.authentication
{
    public class FormsAuthenticationSource: IAuthenticationSource
    {
        CommunityContext Community;

        public FormsAuthenticationSource(CommunityContext community)
        {
            Community = community;
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

            //for (int i = 0; i < MinimumNonAlphanumericLength; i++)
            //{
            //    chars[i] = _allowedChars[(int)((_allowedChars.Length) * randNum.NextDouble())];
            //}
            
            return new string(chars);
        }

        private string processPasswordForStorage(string password)
        {
            password = FormsAuthentication.HashPasswordForStoringInConfigFile(password, FormsAuthPasswordFormat.SHA1.ToString());
            return password;
        }

        #endregion

        public bool ChangePassword(int resourceID, string newPassword)
        {
            var success = false;

            Resource resource = null;

            try
            {
                if (checkPasswordRequirements(newPassword))
                {
                    resource = Community.GetById<Resource>(resourceID); //Repo.GetById(resourceID);
                    if (resource != null)
                    {
                        resource.Password = processPasswordForStorage(newPassword);
                        Community.Update<Resource>(resource); //Repo.SaveOrUpdate(resource);
                        success = true;
                    }
                }
            }
            catch
            {

            }

            return success;
        }

        public string ResetPassword(int resourceID)
        {
            string newPassword = string.Empty;

            Resource resource = null;

            try
            {
                resource = Community.GetById<Resource>(resourceID); //Repo.GetById(resourceID);
                if (resource == null)
                {
                    throw new ApplicationException("No resource found.");
                }

                newPassword = createRandomPassword();
                if (checkPasswordRequirements(newPassword))
                {
                    resource.Password = processPasswordForStorage(newPassword);
                    Community.Update<Resource>(resource); //Repo.SaveOrUpdate(resource);
                }
            }
            catch
            {
                throw;
            }

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
            Resource resource = null;

            try
            {
                password = processPasswordForStorage(password);
                resource = Community.Filter<Resource>(i => i.Username == username && i.Password == password).SingleOrDefault();
                if (resource != null)
                {
                    var companyID = Community.CurrentCompanyID;
                    if (Community.Filter<CompanyResource>(i => i.CompanyID == companyID && i.ResourceID == resource.ID).Any())
                    {
                        FormsAuthentication.SetAuthCookie(resource.Username, true);
                        FormsAuthentication.RedirectFromLoginPage(resource.Username, false);

                        try
                        {
                            resource.DateLastLoggedIn = DateTime.UtcNow;
                            Community.Update<Resource>(resource);
                        }
                        catch {}
                    }
                    else
                    {
                        resource = null;
                    }
                }
            }
            catch
            {
            }

            return resource;
        }
        
        public Resource AddResource(string username, string firstName, string lastName)
        {
            throw new NotImplementedException();
        }
    }
}
