using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class DuplicateTemplateException : BaseException
    {
        public DuplicateTemplateException(string name, string action)
            :base(HttpStatusCode.Conflict, "Duplicate Template Found", string.Format("This template could not be added or updated because a template with the same name already exists ({0} {1}).", name, action))
        {
        }
    }


}
