using System;

namespace d360.web.Models
{
    public class ClientUserModel
    {
        public Guid TenantId { get; set; }

        public string TenantName { get; set; }

        public Guid UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public bool IsAdministrator { get; set; }
    }
}
