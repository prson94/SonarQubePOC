using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class CurrentUserInfo
    {
        public string Identifier { get; set; }
        public UserIdentifierType Type { get; set; }
    }
}
