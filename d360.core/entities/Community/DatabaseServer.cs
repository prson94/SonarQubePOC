using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DatabaseServer : BaseIntObject, IIntObject
    {
        public string Server { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string SearchServer { get; set; }
    }
}
