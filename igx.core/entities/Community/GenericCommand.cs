using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;
using d360.core.enums;
using Newtonsoft.Json;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class GenericCommand : BaseIntObject, IIntObject
    {
        public string Description { get; set; }

        public string CommandText { get; set; }

        public int CommandTimeout { get; set; } = 300;

        public string EnvironmentsLimitJson { get; set; }

        public string ClientsLimitJson { get; set; }

        [NotMapped]
        public List<EnvironmentLevel> EnvironmentsLimit { get
            {
                if (string.IsNullOrEmpty(EnvironmentsLimitJson)) EnvironmentsLimitJson = "[]";
                return JsonConvert.DeserializeObject<List<EnvironmentLevel>>(EnvironmentsLimitJson);
            }
        }

        [NotMapped]
        public List<int> ClientsLimit
        {
            get
            {
                if (string.IsNullOrEmpty(ClientsLimitJson)) ClientsLimitJson = "[]";
                return JsonConvert.DeserializeObject<List<int>>(ClientsLimitJson);
            }
        }
    }
}
