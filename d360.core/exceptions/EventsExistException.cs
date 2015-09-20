using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class EventsExistException : BaseException
    {
        public EventsExistException()
            :base(HttpStatusCode.Conflict, "Existing Events Found", "Item could not be removed because there are existing events associated with it.")
        {
        }
    }


}
