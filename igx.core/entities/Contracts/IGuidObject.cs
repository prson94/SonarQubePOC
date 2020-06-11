using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Contracts
{
    public interface IGuidObject
    {
        Guid ID { get; set; }
    }
}
