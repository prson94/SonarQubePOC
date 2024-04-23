using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract]
    public class CompanyDigestExecution : BaseIntObject
	{
		[DataMember, Key]
        public int CompanyID { get; set; }

		[DataMember]
		public Guid InstanceID { get; set; }

        [DataMember]
        public DateTime? LastExecuted { get; set; }
    }
}
