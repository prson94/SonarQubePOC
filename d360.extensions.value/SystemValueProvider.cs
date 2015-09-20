using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace d360.extensions.value
{
    public class SystemValueProvider : ISystemValueProvider
    {
        public string CurrentUsername
        {
            get 
            {
                return HttpContext.Current.User.Identity.Name.ToLower();
            }
        }
    }
}
