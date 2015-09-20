using d360.core.entities;

namespace d360.extensions
{
    public interface IAuthenticationSource
    {
        bool ChangePassword(int resourceID, string newPassword);
        int GetResourceIDByUsername(string username);
        string ResetPassword(int resourceID);
        Resource AddResource(string username, string firstName, string lastName);
        Resource FindAuthenticatedResource(string username);
        Resource ValidateResource(string username, string password);
    }
}
