using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class IntersectTypeConstraintNotValidException : BaseException
    {
        public IntersectTypeConstraintNotValidException()
            :base(HttpStatusCode.Conflict, "Constraint Set Not Valid", string.Format("{0} are not valid.", "The constraints you specified"))
        {
        }
    }


}
