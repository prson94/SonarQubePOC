using System;
using d360.core.entities;
using d360.model;
using System.Linq;

namespace d360.extensions.authentication
{
    public class KeypairAuthenticationSource : IAuthenticationSource
    {
        CommunityContext Community;

        public KeypairAuthenticationSource(CommunityContext community)
        {
            Community = community;
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
            return Community.Filter<Resource>(i => i.APIPublicKey == username).Single().ID;
        }

        public Resource FindAuthenticatedResource(string username)
        {
            return Community.Filter<Resource>(i => i.APIPublicKey == username, i => i.ResourceType).SingleOrDefault();
        }

        public Resource ValidateResource(string username, string password)
        {
            //int minSeconds = -10;
            //int maxSeconds = 10;

            Resource resource = null;

            try
            {
                resource = Community.Filter<Resource>(i => i.APIPublicKey == username, i => i.ResourceType).SingleOrDefault();

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

        public Resource AddResource(string username, string firstName, string lastName)
        {
            throw new NotImplementedException();
        }
    }
}
