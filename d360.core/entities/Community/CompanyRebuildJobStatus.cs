using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RebuildJobStatus : BaseObject
    {
        [DataMember, Key, Column(Order = 2)]
        public CompanyRebuildJobToken JobToken { get; set; }

        [DataMember]
        public DateTime LastStartedOn { get; set; }

        [DataMember]
        public int LastStartedBy { get; set; }

        [DataMember]
        public DateTime? LastCompletedOn { get; set; }

        [DataMember]
        public CompanyRebuildJobStatusState State { get; set; }
    }

    [DataContract]
    public class CompanyRebuildJobStatusApiModel
    {
        [DataMember(Name = "jobToken")]
        public CompanyRebuildJobToken JobToken { get; set; }
        [DataMember(Name = "jobTokenName")]
        public string JobTokenName { get; set; }
        [DataMember(Name = "jobTokenDescription")]
        public string JobTokenDescription { get; set; }
        [DataMember(Name = "state")]
        public CompanyRebuildJobStatusState State { get; set; }
        [DataMember(Name = "lastStartedOn")]
        public DateTime? LastStartedOn { get; set; }
        [DataMember(Name = "lastCompletedOn")]
        public DateTime? LastCompletedOn { get; set; }

        public static List<CompanyRebuildJobStatusApiModel> GetDefaultList()
        {
            return CompanyRebuildJobToken.AssetGraph
                .GetAsList()
                .Select(i => new CompanyRebuildJobStatusApiModel
                {
                    JobTokenDescription = i.Description,
                    JobTokenName = i.Name,
                    JobToken = i.ID,
                    State = CompanyRebuildJobStatusState.Inactive
                }).ToList();
        }

        public void SetCurrentJobStatusProperties(RebuildJobStatus current)
        {
            this.LastCompletedOn = current.LastCompletedOn;
            this.LastStartedOn = current.LastStartedOn;
            this.State = current.State;
        }
    }
}
