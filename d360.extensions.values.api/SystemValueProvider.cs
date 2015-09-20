using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.services.interfaces;

namespace d360.extensions.values.api
{
    public class SystemValueProvider : ISystemValueProvider
    {
        public string CurrentUsername
        {
            get
            {
                string[] values = HttpContext.Current.Request.Headers["Authorization"].Split(';');
                return values[1];
            }
        }
    }
}
