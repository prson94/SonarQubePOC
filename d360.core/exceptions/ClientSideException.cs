using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.exceptions
{
    public class ClientSideException : ApplicationException
    {
        public ClientSideException() { }
        public ClientSideException(string message) : base(message)
        {}

        public ClientSideException(string message,Exception inner): base(message, inner)
        { }
    }
}
