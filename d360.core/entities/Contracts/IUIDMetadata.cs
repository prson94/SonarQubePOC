using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Contracts
{
    /// <summary>
    /// This interface is used in the DbContext when checking whether object have these UID field present.  
    /// If so, this tells the DbContext to insert value on UID.  
    /// </summary>
   public interface IUIDMetadata
    {
         Guid? UID { get; set; }
    }
}
