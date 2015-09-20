using System;
using d360.core.entities;
using d360.services.interfaces;

namespace d360.extensions.security.keypair
{
    public class AuthenticationSource : IAuthenticationSource
    {
        ISecurityService Security;

        public AuthenticationSource(ISecurityService security)
        {
            Security = security;
        }

        public bool ChangePassword(int resourceID, string newPassword)
        {
            throw new NotImplementedException();
        }

        public string ResetPassword(int resourceID)
        {
            throw new NotImplementedException();
        }

        public int GetResourceIDByUsername(string username)
        {
            return Security.GetResourceIDByAPIKey(username);
        }

        public Resource FindAuthenticatedResource(string username)
        {
            return Security.GetApiResource(username);
        }

        public Resource ValidateResource(string username, string password)
        {
            //int minSeconds = -10;
            //int maxSeconds = 10;

            Resource resource = null;

            try
            {
                resource = Security.GetApiResource(username);

                if (resource != null)
                {
                    //long secondsSinceEpoch = DateTime.UtcNow.Date.Epoch();
                    string secretKey = resource.APIPrivateKey;

                    //var hash = new SHA256Managed();

                    bool isAuthorized = false;

                    //for (long i = (secondsSinceEpoch + minSeconds); i <= (secondsSinceEpoch + maxSeconds); i++)
                    //{
                    //string correctHash = secretKey + secondsSinceEpoch.ToString();
                    //byte[] unhashedBytes = Encoding.ASCII.GetBytes(correctHash); //encoding.GetBytes(correctHash);
                    //byte[] hashedBytes = hash.ComputeHash(unhashedBytes);
                    //correctHash = Convert.ToBase64String(hashedBytes);

                    //if (correctHash.Equals(password))
                    //{
                    //    isAuthorized = true;
                    //break;
                    //}
                    ///}

                    isAuthorized = secretKey.Equals(password);

                    if (!isAuthorized)
                    {
                        resource = null;
                    }
                }
            }
            catch// (Exception ex)
            {
                resource = null;
            }

            return resource;
        }
        
        public void Signout(string username)
        {
            //FormsAuthentication.SignOut();
        }
    }
}
