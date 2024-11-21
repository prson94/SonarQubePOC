using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
	[Table("OpenIdRequest", Schema = "dbo")]
    public class OpenIdRequest
    {
        [Key]
        public string State { get; set; }

        public string Nonce { get; set; }

        public string RedirectUrl { get; set; }
    }
}
