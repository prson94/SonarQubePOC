using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    public class OpenIdRequest
    {
        [Key]
        public string State { get; set; }

        public string Nonce { get; set; }

        public string RedirectUrl { get; set; }
    }
}
