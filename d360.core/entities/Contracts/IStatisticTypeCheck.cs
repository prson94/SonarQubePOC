using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Contracts
{
    public interface IStatisticTypeCheck
    {
        int CompanyID { get; set; }

        int StatisticTypeID { get; set; }

        //StatisticType StatisticType { get; set; } 
    }
}
